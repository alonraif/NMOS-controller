using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Topology;
using NmosController.Domain.Enums;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Nmos.Clients;
using NmosController.Infrastructure.Nmos.Mock;

namespace NmosController.Infrastructure.Nmos.Switching;

internal sealed class SwitchingNmosQueryClient(
    IRegistrySettingsResolver registrySettingsResolver,
    NmosQueryApiClient liveClient,
    MockNmosQueryClient mockClient) : INmosQueryClient
{
    public async Task<TopologySnapshotDto> GetTopologySnapshotAsync(CancellationToken cancellationToken)
    {
        var settings = await registrySettingsResolver.GetAsync(cancellationToken);
        return settings.Mode == ControllerMode.Mock
            ? await mockClient.GetTopologySnapshotAsync(cancellationToken)
            : await liveClient.GetTopologySnapshotAsync(cancellationToken);
    }
}
