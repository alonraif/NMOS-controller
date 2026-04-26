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
    private static readonly TimeSpan ManifestRequestTimeout = TimeSpan.FromSeconds(3);
    private const int ManifestRequestAttempts = 2;

    public async Task<TopologySnapshotDto> GetTopologySnapshotAsync(CancellationToken cancellationToken)
    {
        var registry = await registrySettingsResolver.GetAsync(cancellationToken);
        var queryBase = new Uri(registry.BaseUrl, $"/x-nmos/query/{registry.QueryApiVersion.TrimStart('/')}/");

        logger.LogInformation("Fetching NMOS topology snapshot from {RegistryBaseUrl}", queryBase);

        var nodes = await GetPagedAsync<NmosNodeResourceDto>(new Uri(queryBase, "nodes"), cancellationToken);
        var devices = await GetPagedAsync<NmosDeviceResourceDto>(new Uri(queryBase, "devices"), cancellationToken);
        var sources = await GetPagedAsync<NmosSourceResourceDto>(new Uri(queryBase, "sources"), cancellationToken);
        var flows = await GetPagedAsync<NmosFlowResourceDto>(new Uri(queryBase, "flows"), cancellationToken);
        var senders = await GetPagedAsync<NmosSenderResourceDto>(new Uri(queryBase, "senders"), cancellationToken);
        var receivers = await GetPagedAsync<NmosReceiverResourceDto>(new Uri(queryBase, "receivers"), cancellationToken);

        var devicesById = devices.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var connectionBaseByDeviceId = devices.ToDictionary(
            x => x.Id,
            ResolveConnectionBaseUrl,
            StringComparer.OrdinalIgnoreCase);
        var candidateConnectionBaseUrls = BuildCandidateConnectionBaseUrls(
            devices,
            registry.ConnectionBaseUrl,
            registry.ConnectionBaseUrls);
        var flowsById = flows.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var senderDtos = await Task.WhenAll(
            senders.Select(async sender =>
            {
                var device = devicesById.GetValueOrDefault(sender.DeviceId);
                var flow = sender.FlowId is not null ? flowsById.GetValueOrDefault(sender.FlowId) : null;
                var flowDto = flow?.ToDto();
                var transportFile = await TryFetchTransportFileAsync(sender.ManifestHref, cancellationToken);

                return sender.ToDto(
                    device?.NodeId ?? string.Empty,
                    flowDto?.Format ?? new MediaFormatSummary(string.Empty, null, null, null, null, null),
                    transportFile,
                    InferSignalType(flowDto?.Format.Format),
                    sender.Id,
                    sender.Label,
                    null,
                    "A",
                    true);
            }));

        var receiverDtos = await Task.WhenAll(
            receivers.Select(async receiver =>
            {
                var device = devicesById.GetValueOrDefault(receiver.DeviceId);
                var receiverConnectionBaseUrl = connectionBaseByDeviceId.GetValueOrDefault(receiver.DeviceId);
                var effectiveConnectionBaseUrl = receiverConnectionBaseUrl
                    ?? candidateConnectionBaseUrls.FirstOrDefault()
                    ?? registry.ConnectionBaseUrl.ToString();
                var receiverState = await connectionClient.GetReceiverStateAsync(receiver.Id, effectiveConnectionBaseUrl, cancellationToken);

                return receiver.ToDto(
                    device?.NodeId ?? string.Empty,
                    receiverState?.Constraints ?? ConstraintSet.Empty,
                    receiverState?.Active ?? new ConnectionState(null, null, new Dictionary<string, string>(), null),
                    receiverState?.Staged ?? new ConnectionState(null, null, new Dictionary<string, string>(), null),
                    InferSignalType(receiver.Format),
                    receiver.Id,
                    receiver.Label,
                    effectiveConnectionBaseUrl);
            }));

        return new TopologySnapshotDto(
            new RegistrySummaryDto(
                Guid.Empty,
                registry.Name,
                registry.BaseUrl.ToString(),
                registry.QueryApiVersion,
                registry.ConnectionApiVersion,
                registry.IsEnabled),
            nodes.Select(x => x.ToDto()).ToArray(),
            devices.Select(x => x.ToDto()).ToArray(),
            sources.Select(x => x.ToDto()).ToArray(),
            flows.Select(x => x.ToDto()).ToArray(),
            senderDtos,
            receiverDtos,
            receiverDtos.Select(x => new RoutingDestinationSnapshotDto(x.RoutingDestinationId, x.RoutingDestinationLabel, x.NodeId, x.DeviceId, x.SignalType == "Video" ? x.Id : null, x.SignalType == "Audio" ? x.Id : null, x.SignalType == "Ancillary" ? x.Id : null, Array.Empty<string>())).ToArray(),
            DateTimeOffset.UtcNow);
    }

    private static string InferSignalType(string? format) =>
        format switch
        {
            "urn:x-nmos:format:video" => "Video",
            "urn:x-nmos:format:audio" => "Audio",
            "urn:x-nmos:format:data" => "Ancillary",
            _ => "Unknown"
        };

    private static IReadOnlyCollection<string> BuildCandidateConnectionBaseUrls(
        IReadOnlyCollection<NmosDeviceResourceDto> devices,
        Uri registryConnectionBaseUrl,
        string? configuredConnectionBaseUrls)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            if (seen.Add(candidate))
            {
                candidates.Add(candidate);
            }
        }

        foreach (var device in devices)
        {
            var controlBaseUrl = ResolveConnectionBaseUrl(device);
            AddCandidate(controlBaseUrl);
        }

        if (!string.IsNullOrWhiteSpace(configuredConnectionBaseUrls))
        {
            foreach (var candidate in configuredConnectionBaseUrls.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (Uri.TryCreate(candidate, UriKind.Absolute, out var parsed))
                {
                    AddCandidate($"{parsed.Scheme}://{parsed.Authority}");
                }
            }
        }

        AddCandidate($"{registryConnectionBaseUrl.Scheme}://{registryConnectionBaseUrl.Authority}");
        return candidates.ToArray();
    }

    internal static string? ResolveConnectionBaseUrl(NmosDeviceResourceDto device)
    {
        var controls = device.Controls;
        if (controls is null || controls.Count == 0)
        {
            return null;
        }

        static bool IsPreferredConnectionControlType(string? controlType)
        {
            if (string.IsNullOrWhiteSpace(controlType))
            {
                return false;
            }

            return controlType.Contains("urn:x-nmos:control:sr-ctrl/", StringComparison.OrdinalIgnoreCase)
                || controlType.Contains("urn:x-nmos:control:cm-ctrl/", StringComparison.OrdinalIgnoreCase);
        }

        var preferredControl = controls.FirstOrDefault(x => IsPreferredConnectionControlType(x.Type))
            ?? controls.FirstOrDefault(x =>
                x.Href is not null && x.Href.Contains("/x-nmos/connection/", StringComparison.OrdinalIgnoreCase));

        if (!Uri.TryCreate(preferredControl?.Href, UriKind.Absolute, out var controlUri))
        {
            return null;
        }

        return $"{controlUri.Scheme}://{controlUri.Authority}";
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

    private async Task<List<TItem>> GetPagedAsync<TItem>(Uri firstPageUri, CancellationToken cancellationToken)
    {
        var aggregated = new List<TItem>();
        var currentPageUri = firstPageUri;
        var seenPageUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isInitialRequest = true;

        while (seenPageUris.Add(currentPageUri.AbsoluteUri))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentPageUri);
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "NMOS GET {Uri} failed with status {StatusCode}. Body: {Payload}",
                    currentPageUri,
                    (int)response.StatusCode,
                    payload);

                response.EnsureSuccessStatusCode();
            }

            var page = await response.Content.ReadFromJsonAsync<List<TItem>>(NmosJsonSerializer.Default, cancellationToken) ?? [];
            aggregated.AddRange(page);

            // Some registries return the most recent page for a bare collection request.
            // In that case we should jump to rel="first" and iterate forward with rel="next".
            if (isInitialRequest)
            {
                isInitialRequest = false;
                var firstLink = TryGetLinkUri(response, firstPageUri, "first");
                if (firstLink is not null && !string.Equals(firstLink.AbsoluteUri, currentPageUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
                {
                    aggregated.Clear();
                    currentPageUri = firstLink;
                    continue;
                }
            }

            var nextLink = TryGetLinkUri(response, firstPageUri, "next");
            if (nextLink is null)
            {
                break;
            }

            currentPageUri = nextLink;
        }

        return aggregated;
    }

    private static Uri? TryGetLinkUri(HttpResponseMessage response, Uri requestUri, string relation)
    {
        if (!response.Headers.TryGetValues("Link", out var linkHeaders))
        {
            return null;
        }

        foreach (var headerValue in linkHeaders)
        {
            foreach (var rawSegment in headerValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!rawSegment.Contains($"rel=\"{relation}\"", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var startIndex = rawSegment.IndexOf('<');
                var endIndex = rawSegment.IndexOf('>');
                if (startIndex < 0 || endIndex <= startIndex + 1)
                {
                    continue;
                }

                var nextUriCandidate = rawSegment[(startIndex + 1)..endIndex];
                if (Uri.TryCreate(nextUriCandidate, UriKind.Absolute, out var absoluteUri))
                {
                    return absoluteUri;
                }

                if (Uri.TryCreate(requestUri, nextUriCandidate, out var relativeUri))
                {
                    return relativeUri;
                }
            }
        }

        return null;
    }

    private async Task<TransportFileData?> TryFetchTransportFileAsync(string? manifestHref, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifestHref))
        {
            return null;
        }

        for (var attempt = 1; attempt <= ManifestRequestAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ManifestRequestTimeout);

            try
            {
                using var response = await httpClient.GetAsync(manifestHref, timeoutCts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    if (attempt == ManifestRequestAttempts)
                    {
                        logger.LogWarning(
                            "Failed to fetch sender transport file from {ManifestHref}. Status {StatusCode}",
                            manifestHref,
                            (int)response.StatusCode);
                        return null;
                    }

                    continue;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/sdp";
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return new TransportFileData(contentType, content);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == ManifestRequestAttempts)
                {
                    logger.LogWarning(
                        "Timed out fetching sender transport file from {ManifestHref} after {Attempts} attempts. Continuing without transport file.",
                        manifestHref,
                        ManifestRequestAttempts);
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (attempt == ManifestRequestAttempts)
                {
                    logger.LogWarning(ex, "Failed to fetch sender transport file from {ManifestHref}", manifestHref);
                    return null;
                }
            }
        }

        return null;
    }
}
