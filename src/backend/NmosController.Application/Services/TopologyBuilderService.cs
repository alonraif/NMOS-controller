using NmosController.Application.Topology;

namespace NmosController.Application.Services;

public sealed class TopologyBuilderService
{
    public TopologyGraphDto BuildGraph(TopologySnapshotDto snapshot)
    {
        var sendersById = snapshot.Senders.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var senderGroups = snapshot.Senders
            .GroupBy(x => x.SourceGroupId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);

        var edges = new List<TopologyRouteEdgeDto>();
        foreach (var destination in snapshot.RoutingDestinations)
        {
            BuildEdgesForReceiver(destination, destination.VideoReceiverId, "Video", sendersById, senderGroups, snapshot.Receivers, edges);
            BuildEdgesForReceiver(destination, destination.AudioReceiverId, "Audio", sendersById, senderGroups, snapshot.Receivers, edges);
            BuildEdgesForReceiver(destination, destination.AncillaryReceiverId, "Ancillary", sendersById, senderGroups, snapshot.Receivers, edges);
        }

        return new TopologyGraphDto(
            snapshot.Registry,
            snapshot.Nodes,
            snapshot.Devices,
            snapshot.Sources,
            snapshot.Flows,
            snapshot.Senders,
            snapshot.Receivers,
            snapshot.RoutingDestinations,
            edges,
            snapshot.RetrievedAtUtc);
    }

    private static void BuildEdgesForReceiver(
        RoutingDestinationSnapshotDto destination,
        string? receiverId,
        string layer,
        IReadOnlyDictionary<string, NmosSenderDto> sendersById,
        IReadOnlyDictionary<string, NmosSenderDto[]> senderGroups,
        IReadOnlyCollection<NmosReceiverDto> receivers,
        ICollection<TopologyRouteEdgeDto> edges)
    {
        if (string.IsNullOrWhiteSpace(receiverId))
        {
            return;
        }

        var receiver = receivers.FirstOrDefault(x => string.Equals(x.Id, receiverId, StringComparison.OrdinalIgnoreCase));
        if (receiver is null)
        {
            return;
        }

        AppendEdges(receiver.Active.SenderId, "active");
        if (!string.Equals(receiver.Staged.SenderId, receiver.Active.SenderId, StringComparison.OrdinalIgnoreCase))
        {
            AppendEdges(receiver.Staged.SenderId, "staged");
        }

        void AppendEdges(string? senderId, string state)
        {
            if (string.IsNullOrWhiteSpace(senderId) || !sendersById.TryGetValue(senderId, out var sender))
            {
                return;
            }

            if (!senderGroups.TryGetValue(sender.SourceGroupId, out var groupedSenders))
            {
                groupedSenders = [sender];
            }

            foreach (var groupedSender in groupedSenders.OrderBy(x => x.PathType))
            {
                edges.Add(
                    new TopologyRouteEdgeDto(
                        $"{destination.Id}:{layer}:{state}:{groupedSender.PathType}",
                        groupedSender.Id,
                        destination.Id,
                        state,
                        groupedSender.PathType,
                        layer,
                        groupedSender.RedundancyGroupId,
                        groupedSender.IsHealthy,
                        new Dictionary<string, string>
                        {
                            ["receiverId"] = receiver.Id,
                            ["senderGroupId"] = groupedSender.SourceGroupId
                        }));
            }
        }
    }
}
