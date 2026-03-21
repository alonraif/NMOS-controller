using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NmosController.Application.Abstractions.Integrations;
using NmosController.Domain.Entities;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Json;
using NmosController.Infrastructure.Nmos.Dtos.Is05;
using NmosController.Infrastructure.Nmos.Mapping;

namespace NmosController.Infrastructure.Nmos.Clients;

internal sealed class NmosConnectionApiClient(
    HttpClient httpClient,
    IRegistrySettingsResolver registrySettingsResolver,
    ILogger<NmosConnectionApiClient> logger) : INmosConnectionClient
{
    public async Task ApplyConnectionAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        var registry = await registrySettingsResolver.GetAsync(cancellationToken);
        var stagedEndpoint = new Uri(
            registry.BaseUrl,
            $"/x-nmos/connection/{registry.ConnectionApiVersion.TrimStart('/')}/single/receivers/{request.ReceiverId}/staged");

        var payload = new NmosConnectionPatchRequestDto
        {
            SenderId = request.Operation == Domain.Enums.ConnectionOperation.Connect ? request.SenderId : null,
            MasterEnable = request.Operation == Domain.Enums.ConnectionOperation.Connect,
            Activation = new NmosPatchActivationDto
            {
                Mode = NmosResourceMapper.ToActivationModeString(request.Activation),
                RequestedTime = request.Activation.ActivationTimeUtc?.UtcDateTime.ToString("O")
            }
        };

        using var response = await httpClient.PatchAsJsonAsync(stagedEndpoint, payload, NmosJsonSerializer.Default, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "NMOS connection PATCH failed for receiver {ReceiverId} with status {StatusCode}. Body: {Body}",
                request.ReceiverId,
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<NmosReceiver?> GetReceiverStateAsync(string receiverId, CancellationToken cancellationToken)
    {
        var registry = await registrySettingsResolver.GetAsync(cancellationToken);
        var baseUri = new Uri(
            registry.BaseUrl,
            $"/x-nmos/connection/{registry.ConnectionApiVersion.TrimStart('/')}/single/receivers/{receiverId}/");

        var constraints = await GetAsync<NmosReceiverConstraintsDto>(new Uri(baseUri, "constraints"), cancellationToken);
        var active = await GetAsync<NmosConnectionStateDto>(new Uri(baseUri, "active"), cancellationToken);
        var staged = await GetAsync<NmosConnectionStateDto>(new Uri(baseUri, "staged"), cancellationToken);

        return NmosResourceMapper.ToDomainReceiver(
            receiverId,
            NmosResourceMapper.MapConstraints(constraints, NmosResourceMapper.ParseTransport(null)),
            NmosResourceMapper.MapConnectionState(active),
            NmosResourceMapper.MapConnectionState(staged));
    }

    private async Task<T?> GetAsync<T>(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError(
                "NMOS GET {Uri} failed with status {StatusCode}. Body: {Payload}",
                uri,
                (int)response.StatusCode,
                payload);
            response.EnsureSuccessStatusCode();
        }

        return await response.Content.ReadFromJsonAsync<T>(NmosJsonSerializer.Default, cancellationToken);
    }
}
