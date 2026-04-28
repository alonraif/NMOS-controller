using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using NmosController.Application.Observability;
using NmosController.Contracts.Responses;
using System.Diagnostics;
using System.Globalization;

namespace NmosController.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
public sealed class SystemController : ControllerBase
{
    private static readonly object CpuSampleLock = new();
    private static TimeSpan _lastCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
    private static DateTimeOffset _lastCpuSampleUtc = DateTimeOffset.UtcNow;
    private static long? _lastHostCpuTotalTicks;
    private static long? _lastHostCpuIdleTicks;

    [HttpGet("host")]
    [ProducesResponseType(typeof(ApiEnvelope<HostResourceSnapshotDto>), StatusCodes.Status200OK)]
    public IActionResult GetHostResources()
    {
        var hostCpuInUsePercent = CalculateHostCpuInUsePercent();
        var cpuUsedByControllerPercent = CalculateProcessCpuUsagePercent(100d);
        var cpuAvailablePercent = Math.Clamp(100d - hostCpuInUsePercent, 0d, 100d);

        var (memoryTotalBytes, memoryAvailableBytes) = ReadHostMemoryBytes();
        var memoryUsedByControllerBytes = Process.GetCurrentProcess().WorkingSet64;

        if (memoryTotalBytes > 0)
        {
            memoryUsedByControllerBytes = Math.Min(memoryUsedByControllerBytes, memoryTotalBytes);
            memoryAvailableBytes = Math.Clamp(memoryAvailableBytes, 0L, memoryTotalBytes);
        }

        var payload = new HostResourceSnapshotDto(
            CpuTotalPercent: Math.Round(hostCpuInUsePercent, 2),
            CpuAvailablePercent: Math.Round(cpuAvailablePercent, 2),
            CpuUsedByControllerPercent: Math.Round(cpuUsedByControllerPercent, 2),
            MemoryTotalBytes: memoryTotalBytes,
            MemoryAvailableBytes: memoryAvailableBytes,
            MemoryUsedByControllerBytes: memoryUsedByControllerBytes,
            SampledAtUtc: DateTimeOffset.UtcNow);

        return Ok(new ApiEnvelope<HostResourceSnapshotDto>(payload, DateTimeOffset.UtcNow));
    }

    private static double CalculateHostCpuInUsePercent()
    {
        const string statPath = "/proc/stat";
        if (!System.IO.File.Exists(statPath))
        {
            return 0d;
        }

        var firstLine = System.IO.File.ReadLines(statPath).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstLine) || !firstLine.StartsWith("cpu ", StringComparison.Ordinal))
        {
            return 0d;
        }

        var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
        {
            return 0d;
        }

        long ParsePart(int index) =>
            index < parts.Length && long.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0L;

        var idle = ParsePart(4);
        var iowait = ParsePart(5);
        var irq = ParsePart(6);
        var softirq = ParsePart(7);
        var steal = ParsePart(8);

        var user = ParsePart(1);
        var nice = ParsePart(2);
        var system = ParsePart(3);

        var idleAll = idle + iowait;
        var nonIdle = user + nice + system + irq + softirq + steal;
        var total = idleAll + nonIdle;

        lock (CpuSampleLock)
        {
            if (_lastHostCpuTotalTicks is null || _lastHostCpuIdleTicks is null)
            {
                _lastHostCpuTotalTicks = total;
                _lastHostCpuIdleTicks = idleAll;
                return 0d;
            }

            var totalDelta = total - _lastHostCpuTotalTicks.Value;
            var idleDelta = idleAll - _lastHostCpuIdleTicks.Value;

            _lastHostCpuTotalTicks = total;
            _lastHostCpuIdleTicks = idleAll;

            if (totalDelta <= 0)
            {
                return 0d;
            }

            var inUse = ((double)(totalDelta - idleDelta) / totalDelta) * 100d;
            return Math.Clamp(inUse, 0d, 100d);
        }
    }

    private static double CalculateProcessCpuUsagePercent(double totalCpuPercent)
    {
        lock (CpuSampleLock)
        {
            var now = DateTimeOffset.UtcNow;
            var processCpuTime = Process.GetCurrentProcess().TotalProcessorTime;

            var elapsedMs = (now - _lastCpuSampleUtc).TotalMilliseconds;
            var cpuDeltaMs = (processCpuTime - _lastCpuTime).TotalMilliseconds;

            _lastCpuSampleUtc = now;
            _lastCpuTime = processCpuTime;

            if (elapsedMs <= 0)
            {
                return 0d;
            }

            var rawUsageAcrossAllCores = (cpuDeltaMs / elapsedMs) * 100d;
            var usage = rawUsageAcrossAllCores / Math.Max(Environment.ProcessorCount, 1);
            return Math.Clamp(usage, 0d, totalCpuPercent);
        }
    }

    private static (long TotalBytes, long AvailableBytes) ReadHostMemoryBytes()
    {
        const string memInfoPath = "/proc/meminfo";
        if (System.IO.File.Exists(memInfoPath))
        {
            long totalBytes = 0;
            long availableBytes = 0;

            foreach (var line in System.IO.File.ReadLines(memInfoPath))
            {
                if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                {
                    totalBytes = ParseMeminfoLineToBytes(line);
                }
                else if (line.StartsWith("MemAvailable:", StringComparison.Ordinal))
                {
                    availableBytes = ParseMeminfoLineToBytes(line);
                }

                if (totalBytes > 0 && availableBytes > 0)
                {
                    return (totalBytes, availableBytes);
                }
            }
        }

        var gcInfo = GC.GetGCMemoryInfo();
        var fallbackTotal = gcInfo.TotalAvailableMemoryBytes > 0 ? gcInfo.TotalAvailableMemoryBytes : 0;
        var fallbackUsed = GC.GetTotalMemory(false);
        var fallbackAvailable = Math.Max(fallbackTotal - fallbackUsed, 0);
        return (fallbackTotal, fallbackAvailable);
    }

    private static long ParseMeminfoLineToBytes(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return 0;
        }

        return long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var valueKb)
            ? valueKb * 1024L
            : 0;
    }
}
