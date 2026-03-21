using NmosController.Application.Abstractions.Integrations;
using NmosController.Domain.Entities;

namespace NmosController.Infrastructure.Nmos.Mock;

public sealed class MockNmosConnectionClient(MockNmosFixtureStore fixtureStore) : INmosConnectionClient
{
    public Task ApplyConnectionAsync(ConnectionRequest request, CancellationToken cancellationToken) =>
        fixtureStore.ApplyConnectionAsync(request, cancellationToken);

    public Task<NmosReceiver?> GetReceiverStateAsync(string receiverId, CancellationToken cancellationToken) =>
        fixtureStore.GetReceiverAsync(receiverId, cancellationToken);
}
