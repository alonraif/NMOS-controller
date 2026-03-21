using NmosController.Domain.Entities;
using NmosController.Domain.Enums;

namespace NmosController.Application.Topology;

internal static class TopologyMappingExtensions
{
    public static NmosSender ToDomain(this NmosSenderDto sender) =>
        new()
        {
            Id = sender.Id,
            NodeId = sender.NodeId,
            DeviceId = sender.DeviceId,
            FlowId = sender.FlowId,
            Label = sender.Label,
            Transport = sender.Transport,
            Format = sender.Format,
            ManifestHref = sender.ManifestHref,
            SubscribedReceiverId = sender.SubscribedReceiverId,
            TransportFile = sender.TransportFile,
            LastSeenAtUtc = sender.LastSeenAtUtc
        };

    public static NmosReceiver ToDomain(this NmosReceiverDto receiver) =>
        new()
        {
            Id = receiver.Id,
            NodeId = receiver.NodeId,
            DeviceId = receiver.DeviceId,
            Label = receiver.Label,
            Transport = receiver.Transport,
            Format = receiver.Format,
            Constraints = receiver.Constraints,
            Active = receiver.Active,
            Staged = receiver.Staged,
            IsConnectable = receiver.IsConnectable,
            LastSeenAtUtc = receiver.LastSeenAtUtc
        };
}
