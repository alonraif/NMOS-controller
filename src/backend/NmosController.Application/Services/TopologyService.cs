using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Application.Topology;
using NmosController.Domain.Enums;

namespace NmosController.Application.Services;

public sealed class TopologyService(
    INmosQueryClient queryClient,
    TopologyBuilderService topologyBuilder,
    IMemoryCache memoryCache,
    IAuditService auditService) : ITopologyService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(5);
    private const string CacheKey = "topology.snapshot";
    private const string RegistryConnectivityKey = "topology.registry.connectivity";

    public async Task<TopologyGraphDto> GetTopologyAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(forceRefresh, cancellationToken);
        return topologyBuilder.BuildGraph(snapshot);
    }

    public async Task<IReadOnlyCollection<NmosSenderDto>> GetSendersAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(forceRefresh, cancellationToken);
        return snapshot.Senders.OrderBy(x => x.Label).ToArray();
    }

    public async Task<IReadOnlyCollection<NmosReceiverDto>> GetReceiversAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(forceRefresh, cancellationToken);
        return snapshot.Receivers.OrderBy(x => x.Label).ToArray();
    }

    public async Task<ResourceDetailDto?> GetResourceAsync(string resourceId, CancellationToken cancellationToken)
    {
        var snapshot = await GetSnapshotAsync(false, cancellationToken);

        var node = snapshot.Nodes.FirstOrDefault(x => x.Id == resourceId);
        if (node is not null)
        {
            return new ResourceDetailDto(resourceId, ResourceKind.Node, node);
        }

        var device = snapshot.Devices.FirstOrDefault(x => x.Id == resourceId);
        if (device is not null)
        {
            return new ResourceDetailDto(resourceId, ResourceKind.Device, device);
        }

        var source = snapshot.Sources.FirstOrDefault(x => x.Id == resourceId);
        if (source is not null)
        {
            return new ResourceDetailDto(resourceId, ResourceKind.Source, source);
        }

        var flow = snapshot.Flows.FirstOrDefault(x => x.Id == resourceId);
        if (flow is not null)
        {
            return new ResourceDetailDto(resourceId, ResourceKind.Flow, flow);
        }

        var sender = snapshot.Senders.FirstOrDefault(x => x.Id == resourceId);
        if (sender is not null)
        {
            return new ResourceDetailDto(resourceId, ResourceKind.Sender, sender);
        }

        var receiver = snapshot.Receivers.FirstOrDefault(x => x.Id == resourceId);
        if (receiver is not null)
        {
            return new ResourceDetailDto(resourceId, ResourceKind.Receiver, receiver);
        }

        return null;
    }

    public void InvalidateSnapshot() => memoryCache.Remove(CacheKey);

    private async Task<TopologySnapshotDto> GetSnapshotAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && memoryCache.TryGetValue<TopologySnapshotDto>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.TopologyRefreshStarted,
                "system",
                "Topology refresh started.",
                null,
                "Topology",
                null,
                JsonSerializer.Serialize(new { forceRefresh })),
            cancellationToken);

        try
        {
            var snapshot = await queryClient.GetTopologySnapshotAsync(cancellationToken);
            memoryCache.Set(CacheKey, snapshot, CacheLifetime);
            await RecordConnectivityTransitionAsync(true, cancellationToken);
            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.TopologyRefreshed,
                    "system",
                    "Topology refresh completed.",
                    null,
                    "Topology",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        snapshot.RetrievedAtUtc,
                        Nodes = snapshot.Nodes.Count,
                        Devices = snapshot.Devices.Count,
                        Senders = snapshot.Senders.Count,
                        Receivers = snapshot.Receivers.Count
                    })),
                cancellationToken);
            return snapshot;
        }
        catch (Exception ex)
        {
            await RecordConnectivityTransitionAsync(false, cancellationToken);
            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.TopologyRefreshFailed,
                    "system",
                    "Topology refresh failed.",
                    null,
                    "Topology",
                    null,
                    JsonSerializer.Serialize(new { ex.Message })),
                cancellationToken);
            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.ApiRequestFailed,
                    "system",
                    "Controller-side API request failed while refreshing topology.",
                    null,
                    "ApiRequest",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        Endpoint = "NMOS Query/Connection APIs",
                        StatusCode = TryResolveStatusCode(ex),
                        ex.Message
                    })),
                cancellationToken);
            throw;
        }
    }

    private async Task RecordConnectivityTransitionAsync(bool isOnline, CancellationToken cancellationToken)
    {
        var hadPrevious = memoryCache.TryGetValue<bool>(RegistryConnectivityKey, out var previous);
        memoryCache.Set(RegistryConnectivityKey, isOnline, TimeSpan.FromHours(1));
        if (hadPrevious && previous == isOnline)
        {
            return;
        }

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RegistryConnectivityChanged,
                "system",
                $"Registry connectivity changed to {(isOnline ? "online" : "offline")}.",
                null,
                "Registry",
                null,
                JsonSerializer.Serialize(new { IsOnline = isOnline })),
            cancellationToken);
    }

    private static int? TryResolveStatusCode(Exception ex)
    {
        if (ex is HttpRequestException requestException && requestException.StatusCode.HasValue)
        {
            return (int)requestException.StatusCode.Value;
        }

        if (ex is WebException webException && webException.Response is HttpWebResponse webResponse)
        {
            return (int)webResponse.StatusCode;
        }

        return null;
    }
}
