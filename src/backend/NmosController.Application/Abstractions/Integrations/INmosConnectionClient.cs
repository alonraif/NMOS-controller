using NmosController.Domain.Entities;

namespace NmosController.Application.Abstractions.Integrations;

public interface INmosConnectionClient
{
    Task ApplyConnectionAsync(ConnectionRequest request, CancellationToken cancellationToken);
    Task<NmosReceiver?> GetReceiverStateAsync(string receiverId, CancellationToken cancellationToken);
}
