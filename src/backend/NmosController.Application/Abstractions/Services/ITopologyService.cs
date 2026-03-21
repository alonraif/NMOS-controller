using NmosController.Application.Topology;

namespace NmosController.Application.Abstractions.Services;

public interface ITopologyService
{
    Task<TopologyGraphDto> GetTopologyAsync(bool forceRefresh, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NmosSenderDto>> GetSendersAsync(bool forceRefresh, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<NmosReceiverDto>> GetReceiversAsync(bool forceRefresh, CancellationToken cancellationToken);
    Task<ResourceDetailDto?> GetResourceAsync(string resourceId, CancellationToken cancellationToken);
}
