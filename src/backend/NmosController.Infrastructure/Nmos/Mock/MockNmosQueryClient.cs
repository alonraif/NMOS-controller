using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Topology;

namespace NmosController.Infrastructure.Nmos.Mock;

public sealed class MockNmosQueryClient(MockNmosFixtureStore fixtureStore) : INmosQueryClient
{
    public Task<TopologySnapshotDto> GetTopologySnapshotAsync(CancellationToken cancellationToken) =>
        fixtureStore.GetSnapshotAsync(cancellationToken);
}
