using NmosController.Application.Topology;

namespace NmosController.Application.Abstractions.Integrations;

public interface INmosQueryClient
{
    Task<TopologySnapshotDto> GetTopologySnapshotAsync(CancellationToken cancellationToken);
}
