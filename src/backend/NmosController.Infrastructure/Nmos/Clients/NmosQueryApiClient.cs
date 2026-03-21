using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Topology;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Json;
using NmosController.Infrastructure.Nmos.Dtos.Is04;
using NmosController.Infrastructure.Nmos.Mapping;

namespace NmosController.Infrastructure.Nmos.Clients;

internal sealed class NmosQueryApiClient(
    HttpClient httpClient,
    IRegistrySettingsResolver registrySettingsResolver,
    INmosConnectionClient connectionClient,
    ILogger<NmosQueryApiClient> logger) : INmosQueryClient
{
    public async Task<TopologySnapshotDto> GetTopologySnapshotAsync(CancellationToken cancellationToken)
    {
        var registry = await registrySettingsResolver.GetAsync(cancellationToken);
        var queryBase = new Uri(registry.BaseUrl, $"/x-nmos/query/{registry.QueryApiVersion.TrimStart('/')}/");

        logger.LogInformation("Fetching NMOS topology snapshot from {RegistryBaseUrl}", queryBase);

        var nodes = await GetAsync<List<NmosNodeResourceDto>>(new Uri(queryBase, "nodes"), cancellationToken) ?? [];
        var devices = await GetAsync<List<NmosDeviceResourceDto>>(new Uri(queryBase, "devices"), cancellationToken) ?? [];
        var sources = await GetAsync<List<NmosSourceResourceDto>>(new Uri(queryBase, "sources"), cancellationToken) ?? [];
        var flows = await GetAsync<List<NmosFlowResourceDto>>(new Uri(queryBase, "flows"), cancellationToken) ?? [];
        var senders = await GetAsync<List<NmosSenderResourceDto>>(new Uri(queryBase, "senders"), cancellationToken) ?? [];
        var receivers = await GetAsync<List<NmosReceiverResourceDto>>(new Uri(queryBase, "receivers"), cancellationToken) ?? [];

        var devicesById = devices.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var flowsById = flows.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var senderDtos = new List<NmosSenderDto>(senders.Count);
        foreach (var sender in senders)
        {
            var device = devicesById.GetValueOrDefault(sender.DeviceId);
            var flow = sender.FlowId is not null ? flowsById.GetValueOrDefault(sender.FlowId) : null;
            var transportFile = await TryFetchTransportFileAsync(sender.ManifestHref, cancellationToken);
            senderDtos.Add(
                sender.ToDto(
                    device?.NodeId ?? string.Empty,
                    flow?.ToDto().Format ?? new MediaFormatSummary(string.Empty, null, null, null, null, null),
                    transportFile));
        }

        var receiverDtos = new List<NmosReceiverDto>(receivers.Count);
        foreach (var receiver in receivers)
        {
            var device = devicesById.GetValueOrDefault(receiver.DeviceId);
            var receiverState = await connectionClient.GetReceiverStateAsync(receiver.Id, cancellationToken);
            receiverDtos.Add(
                receiver.ToDto(
                    device?.NodeId ?? string.Empty,
                    receiverState?.Constraints ?? ConstraintSet.Empty,
                    receiverState?.Active ?? new ConnectionState(null, null, new Dictionary<string, string>(), null),
                    receiverState?.Staged ?? new ConnectionState(null, null, new Dictionary<string, string>(), null)));
        }

        return new TopologySnapshotDto(
            new RegistrySummaryDto(
                Guid.Empty,
                registry.Name,
                registry.BaseUrl.ToString(),
                registry.QueryApiVersion,
                registry.ConnectionApiVersion,
                registry.Mode,
                registry.IsEnabled),
            nodes.Select(x => x.ToDto()).ToArray(),
            devices.Select(x => x.ToDto()).ToArray(),
            sources.Select(x => x.ToDto()).ToArray(),
            flows.Select(x => x.ToDto()).ToArray(),
            senderDtos,
            receiverDtos,
            DateTimeOffset.UtcNow);
    }

    private async Task<T?> GetAsync<T>(Uri uri, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await httpClient.SendAsync(request, cancellationToken);

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

    private async Task<TransportFileData?> TryFetchTransportFileAsync(string? manifestHref, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifestHref))
        {
            return null;
        }

        try
        {
            using var response = await httpClient.GetAsync(manifestHref, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to fetch sender transport file from {ManifestHref}. Status {StatusCode}",
                    manifestHref,
                    (int)response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/sdp";
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new TransportFileData(contentType, content);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch sender transport file from {ManifestHref}", manifestHref);
            return null;
        }
    }
}
