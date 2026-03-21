using Microsoft.Extensions.Caching.Memory;
using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Topology;
using NmosController.Domain.Enums;

namespace NmosController.Application.Services;

public sealed class TopologyService(
    INmosQueryClient queryClient,
    TopologyBuilderService topologyBuilder,
    IMemoryCache memoryCache) : ITopologyService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(5);
    private const string CacheKey = "topology.snapshot";

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

    private async Task<TopologySnapshotDto> GetSnapshotAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && memoryCache.TryGetValue<TopologySnapshotDto>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var snapshot = await queryClient.GetTopologySnapshotAsync(cancellationToken);
        memoryCache.Set(CacheKey, snapshot, CacheLifetime);
        return snapshot;
    }
}
