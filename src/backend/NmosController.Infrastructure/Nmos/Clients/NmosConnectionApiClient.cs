using System.Net.Http.Json;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NmosController.Application.Abstractions.Integrations;
using NmosController.Domain.Entities;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Json;
using NmosController.Infrastructure.Nmos.Dtos.Is05;
using NmosController.Infrastructure.Nmos.Mapping;
using NmosController.Infrastructure.Nmos.Parsing;

namespace NmosController.Infrastructure.Nmos.Clients;

internal sealed class NmosConnectionApiClient(
    HttpClient httpClient,
    IRegistrySettingsResolver registrySettingsResolver,
    ILogger<NmosConnectionApiClient> logger) : INmosConnectionClient
{
    private static readonly TimeSpan ReceiverStateRequestTimeout = TimeSpan.FromSeconds(3);
    private const int ReceiverStateRequestAttempts = 2;

    public async Task ApplyConnectionAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        var registry = await registrySettingsResolver.GetAsync(cancellationToken);
        var connectionBaseUrl = ResolveConnectionBaseUri(request.ConnectionApiBaseUrl, registry.ConnectionBaseUrl);
        var stagedEndpoint = new Uri(
            connectionBaseUrl,
            $"/x-nmos/connection/{registry.ConnectionApiVersion.TrimStart('/')}/single/receivers/{request.ReceiverId}/staged");
        var shouldDriveByTransportFile = request.Operation == Domain.Enums.ConnectionOperation.Connect
            && request.TransportFile is not null
            && request.TransportParameters.Count == 0;
        var transportParams = request.Operation == Domain.Enums.ConnectionOperation.Connect && !shouldDriveByTransportFile
            ? BuildTransportParams(request.TransportParameters, request.TransportFile, stagedEndpoint.Host)
            : null;

        var payload = new NmosConnectionPatchRequestDto
        {
            SenderId = request.Operation == Domain.Enums.ConnectionOperation.Connect && !shouldDriveByTransportFile
                ? request.SenderId
                : null,
            MasterEnable = request.Operation == Domain.Enums.ConnectionOperation.Connect,
            TransportParams = transportParams,
            TransportFile = request.Operation == Domain.Enums.ConnectionOperation.Connect && request.TransportFile is not null
                ? new NmosTransportFileDto
                {
                    Type = request.TransportFile.ContentType,
                    Data = request.TransportFile.Content
                }
                : null,
            Activation = new NmosPatchActivationDto
            {
                Mode = NmosResourceMapper.ToActivationModeString(request.Activation),
                RequestedTime = request.Activation.ActivationTimeUtc?.UtcDateTime.ToString("O")
            }
        };

        var requestJson = JsonSerializer.Serialize(payload, NmosJsonSerializer.Default);
        using var message = new HttpRequestMessage(HttpMethod.Patch, stagedEndpoint)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        using var response = await SendAsync(message, stagedEndpoint, cancellationToken);
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

    public async Task<NmosReceiver?> GetReceiverStateAsync(string receiverId, string? connectionApiBaseUrl, CancellationToken cancellationToken)
    {
        var registry = await registrySettingsResolver.GetAsync(cancellationToken);
        var resolvedConnectionBaseUrl = ResolveConnectionBaseUri(connectionApiBaseUrl, registry.ConnectionBaseUrl);
        var baseUri = new Uri(
            resolvedConnectionBaseUrl,
            $"/x-nmos/connection/{registry.ConnectionApiVersion.TrimStart('/')}/single/receivers/{receiverId}/");

        var constraints = await TryGetAsync<NmosReceiverConstraintsDto>(new Uri(baseUri, "constraints"), cancellationToken);
        var active = await TryGetAsync<NmosConnectionStateDto>(new Uri(baseUri, "active"), cancellationToken);
        var staged = await TryGetAsync<NmosConnectionStateDto>(new Uri(baseUri, "staged"), cancellationToken);

        return NmosResourceMapper.ToDomainReceiver(
            receiverId,
            NmosResourceMapper.MapConstraints(constraints, NmosResourceMapper.ParseTransport(null)),
            NmosResourceMapper.MapConnectionState(active),
            NmosResourceMapper.MapConnectionState(staged));
    }

    private static Uri ResolveConnectionBaseUri(string? receiverConnectionBaseUrl, Uri fallbackConnectionBaseUrl) =>
        Uri.TryCreate(receiverConnectionBaseUrl, UriKind.Absolute, out var resolvedConnectionBaseUrl)
            ? resolvedConnectionBaseUrl
            : fallbackConnectionBaseUrl;

    private async Task<T?> GetAsync<T>(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, uri), uri, cancellationToken);
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

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, Uri endpoint, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Timed out while contacting the downstream NMOS Connection API at '{endpoint}'.",
                ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException(
                $"Failed to contact the downstream NMOS Connection API at '{endpoint}'. {ex.Message}",
                ex,
                ex.StatusCode);
        }
    }

    private async Task<T?> TryGetAsync<T>(Uri uri, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= ReceiverStateRequestAttempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ReceiverStateRequestTimeout);

            try
            {
                return await GetAsync<T>(uri, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == ReceiverStateRequestAttempts)
                {
                    logger.LogWarning("NMOS GET {Uri} timed out after {Attempts} attempts. Continuing with partial receiver state.", uri, ReceiverStateRequestAttempts);
                    return default;
                }
            }
            catch (HttpRequestException ex)
            {
                if (attempt == ReceiverStateRequestAttempts)
                {
                    logger.LogWarning(ex, "NMOS GET {Uri} failed after {Attempts} attempts. Continuing with partial receiver state.", uri, ReceiverStateRequestAttempts);
                    return default;
                }
            }
        }

        return default;
    }

    private static IReadOnlyCollection<Dictionary<string, object?>>? BuildTransportParams(
        IReadOnlyCollection<IReadOnlyDictionary<string, string>> transportParameters,
        TransportFileData? transportFile,
        string interfaceIp)
    {
        var parsedTransportParams = SdpTransportParametersParser.BuildPrimaryLeg(transportFile, interfaceIp);
        if (parsedTransportParams is not null)
        {
            if (transportParameters.Count == 0)
            {
                return parsedTransportParams;
            }

            var receiverHints = transportParameters.First();
            if (receiverHints.TryGetValue("interface_ip", out var hintedInterfaceIp)
                && !string.IsNullOrWhiteSpace(hintedInterfaceIp))
            {
                parsedTransportParams.First()["interface_ip"] = hintedInterfaceIp;
            }

            return parsedTransportParams;
        }

        if (transportParameters.Count == 0)
        {
            return null;
        }

        return transportParameters
            .Select(transportParameterSet => transportParameterSet.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertTransportValue(kvp.Value),
                StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    private static object? ConvertTransportValue(string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            return intValue;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
        {
            return longValue;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
        {
            return doubleValue;
        }

        return value;
    }
}
