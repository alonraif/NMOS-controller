using NmosController.Application.Audit;
using NmosController.Application.Presets;
using NmosController.Application.Settings;
using NmosController.Application.Sessions;
using NmosController.Application.Topology;
using NmosController.Domain.Entities;
using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Mappers;

public static class DomainToApplicationMapper
{
    public static RegistrySettingsDto ToDto(this Registry registry) =>
        new(
            registry.Id,
            registry.Name,
            registry.BaseUrl.ToString(),
            registry.QueryApiVersion,
            registry.ConnectionApiVersion,
            registry.Mode,
            registry.IsEnabled,
            registry.UpdatedAtUtc);

    public static RegistrySummaryDto ToSummaryDto(this Registry registry) =>
        new(
            registry.Id,
            registry.Name,
            registry.BaseUrl.ToString(),
            registry.QueryApiVersion,
            registry.ConnectionApiVersion,
            registry.Mode,
            registry.IsEnabled);

    public static AuditEntryDto ToDto(this AuditEntry entry) =>
        new(
            entry.Id,
            entry.ActionType,
            entry.Actor,
            entry.Summary,
            entry.ResourceId,
            entry.ResourceType,
            entry.CorrelationId,
            entry.OccurredAtUtc,
            entry.MetadataJson);

    public static UserSessionDto ToDto(this UserSession session) =>
        new(
            session.Id,
            session.UserName,
            session.DisplayName,
            session.RemoteAddress,
            session.UserAgent,
            session.StartedAtUtc,
            session.EndedAtUtc,
            session.State);

    public static PresetSalvoDto ToDto(this PresetSalvo preset) =>
        new(
            preset.Id,
            preset.Name,
            preset.Description,
            preset.Routes.Select(ToDto).ToArray(),
            preset.CreatedAtUtc,
            preset.UpdatedAtUtc);

    public static PresetRouteDto ToDto(this PresetRoute route) =>
        new(
            route.ReceiverId,
            route.SenderId,
            route.Activation.Mode,
            route.Activation.ActivationTimeUtc,
            route.Activation.RequestedOffset);

    public static NmosNodeDto ToDto(this NmosNode node) =>
        new(
            node.Id,
            node.Label,
            node.Hostname,
            node.Description,
            node.ApiVersions,
            node.Interfaces,
            node.LastSeenAtUtc);

    public static NmosDeviceDto ToDto(this NmosDevice device) =>
        new(
            device.Id,
            device.NodeId,
            device.Label,
            device.DeviceType,
            device.SenderIds,
            device.ReceiverIds,
            device.LastSeenAtUtc);

    public static NmosSourceDto ToDto(this NmosSource source) =>
        new(
            source.Id,
            source.DeviceId,
            source.Label,
            source.Format,
            source.LastSeenAtUtc);

    public static NmosFlowDto ToDto(this NmosFlow flow) =>
        new(
            flow.Id,
            flow.SourceId,
            flow.DeviceId,
            flow.Label,
            flow.Format,
            flow.LastSeenAtUtc);

    public static NmosSenderDto ToDto(this NmosSender sender) =>
        new(
            sender.Id,
            sender.NodeId,
            sender.DeviceId,
            sender.FlowId,
            sender.Label,
            sender.Transport,
            sender.Format,
            sender.ManifestHref,
            sender.SubscribedReceiverId,
            sender.TransportFile,
            GetSignalType(sender.Format),
            sender.Id,
            sender.Label,
            null,
            "A",
            true,
            sender.LastSeenAtUtc);

    public static NmosReceiverDto ToDto(this NmosReceiver receiver) =>
        new(
            receiver.Id,
            receiver.NodeId,
            receiver.DeviceId,
            receiver.Label,
            receiver.Transport,
            receiver.Format,
            receiver.Constraints,
            receiver.Active,
            receiver.Staged,
            receiver.IsConnectable,
            GetSignalType(receiver.Format),
            receiver.Id,
            receiver.Label,
            receiver.LastSeenAtUtc);

    private static string GetSignalType(MediaFormatSummary format) =>
        format.Format switch
        {
            "urn:x-nmos:format:video" => "Video",
            "urn:x-nmos:format:audio" => "Audio",
            "urn:x-nmos:format:data" => "Ancillary",
            _ => "Unknown"
        };
}
