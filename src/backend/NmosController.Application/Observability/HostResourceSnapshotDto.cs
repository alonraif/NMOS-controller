namespace NmosController.Application.Observability;

public sealed record HostResourceSnapshotDto(
    double CpuTotalPercent,
    double CpuAvailablePercent,
    double CpuUsedByControllerPercent,
    long MemoryTotalBytes,
    long MemoryAvailableBytes,
    long MemoryUsedByControllerBytes,
    DateTimeOffset SampledAtUtc);
