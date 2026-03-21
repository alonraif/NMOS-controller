using NmosController.Application.Abstractions.Integrations;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Nmos.Clients;
using NmosController.Infrastructure.Nmos.Mock;

namespace NmosController.Infrastructure.Nmos.Switching;

internal sealed class SwitchingNmosConnectionClient(
    IRegistrySettingsResolver registrySettingsResolver,
    NmosConnectionApiClient liveClient,
    MockNmosConnectionClient mockClient) : INmosConnectionClient
{
    public async Task ApplyConnectionAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        var settings = await registrySettingsResolver.GetAsync(cancellationToken);
        if (settings.Mode == ControllerMode.Mock)
        {
            await mockClient.ApplyConnectionAsync(request, cancellationToken);
            return;
        }

        await liveClient.ApplyConnectionAsync(request, cancellationToken);
    }

    public async Task<NmosReceiver?> GetReceiverStateAsync(string receiverId, CancellationToken cancellationToken)
    {
        var settings = await registrySettingsResolver.GetAsync(cancellationToken);
        return settings.Mode == ControllerMode.Mock
            ? await mockClient.GetReceiverStateAsync(receiverId, cancellationToken)
            : await liveClient.GetReceiverStateAsync(receiverId, cancellationToken);
    }
}
