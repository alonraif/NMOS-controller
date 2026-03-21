using System.Text.Json;
using NmosController.Application.Topology;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Nmos.Dtos.Is04;
using NmosController.Infrastructure.Nmos.Dtos.Is05;

namespace NmosController.Infrastructure.Nmos.Mapping;

internal static class NmosResourceMapper
{
    public static NmosNodeDto ToDto(this NmosNodeResourceDto dto) =>
        new(
            dto.Id,
            dto.Label,
            dto.Hostname,
            dto.Description,
            dto.Api?.Versions?.ToArray() ?? Array.Empty<string>(),
            dto.Interfaces?.Select(x => x.Name ?? x.PortId ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                ?? Array.Empty<string>(),
            DateTimeOffset.UtcNow);

    public static NmosDeviceDto ToDto(this NmosDeviceResourceDto dto) =>
        new(
            dto.Id,
            dto.NodeId,
            dto.Label,
            dto.DeviceType,
            dto.SenderIds?.ToArray() ?? Array.Empty<string>(),
            dto.ReceiverIds?.ToArray() ?? Array.Empty<string>(),
            DateTimeOffset.UtcNow);

    public static NmosSourceDto ToDto(this NmosSourceResourceDto dto) =>
        new(
            dto.Id,
            dto.DeviceId,
            dto.Label,
            new MediaFormatSummary(
                dto.Format,
                null,
                FormatRate(dto.GrainRate),
                null,
                null,
                null),
            DateTimeOffset.UtcNow);

    public static NmosFlowDto ToDto(this NmosFlowResourceDto dto) =>
        new(
            dto.Id,
            dto.SourceId,
            dto.DeviceId,
            dto.Label,
            new MediaFormatSummary(
                dto.Format,
                dto.MediaType,
                FormatRate(dto.GrainRate),
                dto.FrameWidth?.ToString(),
                dto.FrameHeight?.ToString(),
                FormatSampleRate(dto.SampleRate)),
            DateTimeOffset.UtcNow);

    public static NmosSenderDto ToDto(
        this NmosSenderResourceDto dto,
        string nodeId,
        MediaFormatSummary format,
        TransportFileData? transportFile,
        string signalType,
        string sourceGroupId,
        string sourceGroupLabel,
        string? redundancyGroupId,
        string pathType,
        bool isHealthy) =>
        new(
            dto.Id,
            nodeId,
            dto.DeviceId,
            dto.FlowId,
            dto.Label,
            ParseTransport(dto.Transport),
            format,
            dto.ManifestHref,
            dto.Subscription?.ReceiverId,
            transportFile,
            signalType,
            sourceGroupId,
            sourceGroupLabel,
            redundancyGroupId,
            pathType,
            isHealthy,
            DateTimeOffset.UtcNow);

    public static NmosReceiverDto ToDto(
        this NmosReceiverResourceDto dto,
        string nodeId,
        ConstraintSet constraints,
        ConnectionState active,
        ConnectionState staged,
        string signalType,
        string routingDestinationId,
        string routingDestinationLabel) =>
        new(
            dto.Id,
            nodeId,
            dto.DeviceId,
            dto.Label,
            ParseTransport(dto.Transport),
            new MediaFormatSummary(
                dto.Format,
                dto.Caps?.MediaTypes?.FirstOrDefault(),
                null,
                null,
                null,
                null),
            constraints,
            active,
            staged,
            true,
            signalType,
            routingDestinationId,
            routingDestinationLabel,
            DateTimeOffset.UtcNow);

    public static NmosReceiver ToDomainReceiver(
        string receiverId,
        ConstraintSet constraints,
        ConnectionState active,
        ConnectionState staged) =>
        new()
        {
            Id = receiverId,
            Constraints = constraints,
            Active = active,
            Staged = staged,
            LastSeenAtUtc = DateTimeOffset.UtcNow
        };

    public static ConstraintSet MapConstraints(NmosReceiverConstraintsDto? constraints, NmosTransportType transport)
    {
        if (constraints is null || constraints.Count == 0)
        {
            return new ConstraintSet(
                Array.Empty<ConstraintParameter>(),
                Array.Empty<string>(),
                transport == NmosTransportType.Unknown ? Array.Empty<NmosTransportType>() : new[] { transport },
                transport == NmosTransportType.Rtp);
        }

        var parameters = constraints
            .SelectMany(dict => dict)
            .Select(entry =>
            {
                string? minimum = null;
                string? maximum = null;
                IReadOnlyCollection<string> allowedValues = Array.Empty<string>();

                if (entry.Value.ValueKind == JsonValueKind.Object)
                {
                    if (entry.Value.TryGetProperty("minimum", out var min))
                    {
                        minimum = min.ToString();
                    }

                    if (entry.Value.TryGetProperty("maximum", out var max))
                    {
                        maximum = max.ToString();
                    }

                    if (entry.Value.TryGetProperty("enum", out var enumValues) && enumValues.ValueKind == JsonValueKind.Array)
                    {
                        allowedValues = enumValues.EnumerateArray().Select(x => x.ToString()).ToArray();
                    }
                }

                return new ConstraintParameter(entry.Key, minimum, maximum, allowedValues);
            })
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToArray();

        return new ConstraintSet(
            parameters,
            Array.Empty<string>(),
            transport == NmosTransportType.Unknown ? Array.Empty<NmosTransportType>() : new[] { transport },
            transport == NmosTransportType.Rtp);
    }

    public static ConnectionState MapConnectionState(NmosConnectionStateDto? dto)
    {
        if (dto is null)
        {
            return new ConnectionState(null, null, new Dictionary<string, string>(), null);
        }

        var transportParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (dto.TransportParams.ValueKind == JsonValueKind.Array)
        {
            var firstLeg = dto.TransportParams.EnumerateArray().FirstOrDefault();
            if (firstLeg.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in firstLeg.EnumerateObject())
                {
                    transportParameters[property.Name] = property.Value.ToString();
                }
            }
        }

        return new ConnectionState(
            dto.SenderId,
            dto.MasterEnable?.ToString().ToLowerInvariant(),
            transportParameters,
            dto.TransportFile?.Data is null
                ? null
                : new TransportFileData(dto.TransportFile.Type ?? "application/sdp", dto.TransportFile.Data));
    }

    public static string ToActivationModeString(ActivationRequest activation) =>
        activation.Mode switch
        {
            ActivationModeType.ScheduledAbsolute => "activate_scheduled_absolute",
            ActivationModeType.ScheduledRelative => "activate_scheduled_relative",
            _ => "activate_immediate"
        };

    private static string? FormatRate(NmosRateDto? rate)
    {
        if (rate is null)
        {
            return null;
        }

        var denominator = rate.Denominator.GetValueOrDefault(1);
        return $"{rate.Numerator}/{denominator}";
    }

    private static string? FormatSampleRate(NmosSampleRateDto? rate)
    {
        if (rate is null)
        {
            return null;
        }

        var denominator = rate.Denominator.GetValueOrDefault(1);
        return $"{rate.Numerator}/{denominator}";
    }

    public static NmosTransportType ParseTransport(string? transport)
    {
        if (string.IsNullOrWhiteSpace(transport))
        {
            return NmosTransportType.Unknown;
        }

        if (transport.Contains("rtp", StringComparison.OrdinalIgnoreCase))
        {
            return NmosTransportType.Rtp;
        }

        if (transport.Contains("dash", StringComparison.OrdinalIgnoreCase))
        {
            return NmosTransportType.Dash;
        }

        if (transport.Contains("websocket", StringComparison.OrdinalIgnoreCase))
        {
            return NmosTransportType.WebSocket;
        }

        if (transport.Contains("mqtt", StringComparison.OrdinalIgnoreCase))
        {
            return NmosTransportType.Mqtt;
        }

        return NmosTransportType.Unknown;
    }
}
