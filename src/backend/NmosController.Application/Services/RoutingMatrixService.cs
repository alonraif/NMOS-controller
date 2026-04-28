using NmosController.Application.Routing;
using NmosController.Application.Topology;

namespace NmosController.Application.Services;

public sealed class RoutingMatrixService
{
    public RoutingMatrixDto BuildMatrix(TopologySnapshotDto snapshot)
    {
        var senderGroups = snapshot.Senders
            .GroupBy(x => x.SourceGroupId, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.PathType).ToArray();
                var primary = ordered.FirstOrDefault(x => string.Equals(x.PathType, "A", StringComparison.OrdinalIgnoreCase)) ?? ordered.FirstOrDefault();
                var secondary = ordered.FirstOrDefault(x => string.Equals(x.PathType, "B", StringComparison.OrdinalIgnoreCase));
                return new RoutingSourceDto(
                    group.Key,
                    primary?.SourceGroupLabel ?? group.Key,
                    primary?.SourceGroupLabel ?? group.Key,
                    primary?.SignalType ?? "Unknown",
                    primary?.Id,
                    secondary?.Id,
                    GetRedundancyStatus(ordered),
                    ordered.Any(x => x.IsHealthy),
                    primary?.Transport.ToString() ?? "Unknown",
                    primary?.Format.MediaType ?? primary?.Format.Format ?? "Unknown",
                    primary?.NodeId ?? string.Empty,
                    primary?.DeviceId ?? string.Empty);
            })
            .OrderBy(x => x.Layer)
            .ThenBy(x => x.Label)
            .ToArray();

        var sourcesBySenderId = snapshot.Senders.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var sourcesByGroupId = senderGroups.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var receiversById = snapshot.Receivers.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        var destinations = snapshot.RoutingDestinations
            .Select(destination =>
            {
                var routes = new[]
                {
                    BuildDestinationRoute("Video", destination.VideoReceiverId),
                    BuildDestinationRoute("Audio", destination.AudioReceiverId),
                    BuildDestinationRoute("Ancillary", destination.AncillaryReceiverId)
                };

                var activeSourceIds = routes.Where(x => x.ActiveSourceId is not null).Select(x => x.ActiveSourceId!).Distinct(StringComparer.OrdinalIgnoreCase).Take(2).ToArray();
                var normalizedRoutes = routes.Select(route => route with
                {
                    IsBreakaway = route.ActiveSourceId is not null && activeSourceIds.Length > 1
                }).ToArray();

                return new RoutingDestinationDto(
                    destination.Id,
                    destination.Label,
                    destination.NodeId,
                    destination.DeviceId,
                    normalizedRoutes,
                    destination.Tags);

                RoutingDestinationRouteDto BuildDestinationRoute(string layer, string? receiverId)
                {
                    if (string.IsNullOrWhiteSpace(receiverId) || !receiversById.TryGetValue(receiverId, out var receiver))
                    {
                        return new RoutingDestinationRouteDto(layer, false, null, null, null, null, null, null, null, "No signal", false);
                    }

                    var activeSource = ResolveSource(receiver.Active.SenderId);
                    var stagedSource = ResolveSource(receiver.Staged.SenderId);
                    return new RoutingDestinationRouteDto(
                        layer,
                        true,
                        receiver.Id,
                        activeSource?.Id,
                        activeSource?.Label,
                        receiver.Active.SenderId,
                        stagedSource?.Id,
                        stagedSource?.Label,
                        receiver.Staged.SenderId,
                        ResolveRedundancy(receiver.Active.SenderId),
                        false);
                }
            })
            .OrderBy(x => x.Label)
            .ToArray();

        var destinationById = destinations.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        var crosspoints = new List<RoutingCrosspointDto>();
        foreach (var destination in destinations)
        {
            foreach (var source in senderGroups)
            {
                var route = destination.Routes.FirstOrDefault(x => string.Equals(x.Layer, source.Layer, StringComparison.OrdinalIgnoreCase));
                if (route is null)
                {
                    continue;
                }

                var compatible = route.IsSupported;
                crosspoints.Add(
                    new RoutingCrosspointDto(
                        destination.Id,
                        source.Id,
                        source.Layer,
                        compatible,
                        string.Equals(route.ActiveSourceId, source.Id, StringComparison.OrdinalIgnoreCase),
                        route.IsBreakaway));
            }
        }

        return new RoutingMatrixDto(senderGroups, destinations, crosspoints, snapshot.RetrievedAtUtc);

        RoutingSourceDto? ResolveSource(string? senderId)
        {
            if (string.IsNullOrWhiteSpace(senderId) || !sourcesBySenderId.TryGetValue(senderId, out var sender))
            {
                return null;
            }

            return sourcesByGroupId.GetValueOrDefault(sender.SourceGroupId);
        }

        string ResolveRedundancy(string? senderId)
        {
            if (string.IsNullOrWhiteSpace(senderId) || !sourcesBySenderId.TryGetValue(senderId, out var sender))
            {
                return "No signal";
            }

            return senderGroups.First(x => x.Id == sender.SourceGroupId).RedundancyStatus;
        }
    }

    public string ResolveSenderId(TopologySnapshotDto snapshot, string sourceId)
    {
        var candidates = snapshot.Senders
            .Where(x => string.Equals(x.SourceGroupId, sourceId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.IsHealthy)
            .ThenBy(x => x.PathType)
            .ToArray();

        return candidates.FirstOrDefault()?.Id
               ?? throw new InvalidOperationException($"Routing source '{sourceId}' was not found.");
    }

    public RoutingDestinationSnapshotDto ResolveDestination(TopologySnapshotDto snapshot, string destinationId) =>
        snapshot.RoutingDestinations.FirstOrDefault(x => string.Equals(x.Id, destinationId, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Routing destination '{destinationId}' was not found.");

    private static string GetRedundancyStatus(IReadOnlyCollection<NmosSenderDto> senders)
    {
        var hasA = senders.Any(x => string.Equals(x.PathType, "A", StringComparison.OrdinalIgnoreCase) && x.IsHealthy);
        var hasB = senders.Any(x => string.Equals(x.PathType, "B", StringComparison.OrdinalIgnoreCase) && x.IsHealthy);

        return (hasA, hasB) switch
        {
            (true, true) => "A/B OK",
            (true, false) => "A only",
            (false, true) => "B only",
            _ => "No signal"
        };
    }
}
