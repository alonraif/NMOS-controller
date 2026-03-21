using System.Text.Json;
using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Application.Common;
using NmosController.Application.Routing;
using NmosController.Application.Topology;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Domain.Services;

namespace NmosController.Application.Services;

public sealed class RoutingService(
    ITopologyService topologyService,
    INmosConnectionClient connectionClient,
    IAuditService auditService,
    ConnectionCompatibilityEvaluator compatibilityEvaluator,
    RoutingMatrixService routingMatrixService) : IRoutingService
{
    public async Task<RoutingMatrixDto> GetMatrixAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        var topology = await topologyService.GetTopologyAsync(forceRefresh, cancellationToken);
        var snapshot = new TopologySnapshotDto(
            topology.Registry,
            topology.Nodes,
            topology.Devices,
            topology.Sources,
            topology.Flows,
            topology.Senders,
            topology.Receivers,
            topology.RoutingDestinations,
            topology.RefreshedAtUtc);

        return routingMatrixService.BuildMatrix(snapshot);
    }

    public async Task<RouteValidationResultDto> ValidateAsync(RouteValidationCommand command, CancellationToken cancellationToken)
    {
        var topology = await topologyService.GetTopologyAsync(true, cancellationToken);
        var sender = topology.Senders.FirstOrDefault(x => x.Id == command.SenderId);
        var receiver = topology.Receivers.FirstOrDefault(x => x.Id == command.ReceiverId);

        var assessment = compatibilityEvaluator.Evaluate(
            sender?.ToDomain(),
            receiver?.ToDomain(),
            command.Activation);

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.ConnectionValidated,
                "system",
                $"Validated route from sender '{command.SenderId}' to receiver '{command.ReceiverId}'.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { command.SenderId, command.ReceiverId, assessment.Status })),
            cancellationToken);

        return new RouteValidationResultDto(
            assessment.Status,
            assessment.Issues.Select(x => new RouteValidationIssueDto(x.Code, x.Message, x.IsBlocking)).ToArray());
    }

    public async Task<ServiceResult> ConnectAsync(RouteConnectCommand command, CancellationToken cancellationToken)
    {
        var validation = await ValidateAsync(
            new RouteValidationCommand(command.ReceiverId, command.SenderId, command.Activation),
            cancellationToken);

        if (validation.Status == CompatibilityStatus.Incompatible)
        {
            return ServiceResult.Failure("Connection request failed validation.");
        }

        await connectionClient.ApplyConnectionAsync(
            new ConnectionRequest
            {
                Operation = ConnectionOperation.Connect,
                ReceiverId = command.ReceiverId,
                SenderId = command.SenderId,
                Activation = command.Activation,
                RequestedBy = command.RequestedBy,
                RequestedAtUtc = DateTimeOffset.UtcNow
            },
            cancellationToken);

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.ReceiverConnected,
                command.RequestedBy,
                $"Connected sender '{command.SenderId}' to receiver '{command.ReceiverId}'.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { command.SenderId, command.ReceiverId, command.Activation.Mode })),
            cancellationToken);

        return ServiceResult.Success("Connection request submitted.");
    }

    public async Task<ServiceResult> DisconnectAsync(RouteDisconnectCommand command, CancellationToken cancellationToken)
    {
        await connectionClient.ApplyConnectionAsync(
            new ConnectionRequest
            {
                Operation = ConnectionOperation.Disconnect,
                ReceiverId = command.ReceiverId,
                Activation = command.Activation,
                RequestedBy = command.RequestedBy,
                RequestedAtUtc = DateTimeOffset.UtcNow
            },
            cancellationToken);

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.ReceiverDisconnected,
                command.RequestedBy,
                $"Disconnected receiver '{command.ReceiverId}'.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { command.ReceiverId, command.Activation.Mode })),
            cancellationToken);

        return ServiceResult.Success("Disconnect request submitted.");
    }

    public async Task<ServiceResult> ConnectAsync(RoutingConnectCommand command, CancellationToken cancellationToken)
    {
        var topology = await topologyService.GetTopologyAsync(true, cancellationToken);
        var snapshot = new TopologySnapshotDto(
            topology.Registry,
            topology.Nodes,
            topology.Devices,
            topology.Sources,
            topology.Flows,
            topology.Senders,
            topology.Receivers,
            topology.RoutingDestinations,
            topology.RefreshedAtUtc);

        var destination = routingMatrixService.ResolveDestination(snapshot, command.DestinationId);
        var operations = new (string? ReceiverId, string? SourceId, string Layer)[]
        {
            (destination.VideoReceiverId, command.VideoSourceId, "Video"),
            (destination.AudioReceiverId, command.AudioSourceId, "Audio"),
            (destination.AncillaryReceiverId, command.AncillarySourceId, "Ancillary")
        };

        foreach (var operation in operations.Where(x => x.ReceiverId is not null && x.SourceId is not null))
        {
            var senderId = routingMatrixService.ResolveSenderId(snapshot, operation.SourceId!);
            var validation = await ValidateAsync(new RouteValidationCommand(operation.ReceiverId!, senderId, command.Activation), cancellationToken);
            if (validation.Status == CompatibilityStatus.Incompatible)
            {
                return ServiceResult.Failure($"Routing request for {operation.Layer} failed validation.");
            }

            await connectionClient.ApplyConnectionAsync(
                new ConnectionRequest
                {
                    Operation = ConnectionOperation.Connect,
                    ReceiverId = operation.ReceiverId!,
                    SenderId = senderId,
                    Activation = command.Activation,
                    RequestedBy = command.RequestedBy,
                    RequestedAtUtc = DateTimeOffset.UtcNow
                },
                cancellationToken);
        }

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.ReceiverConnected,
                command.RequestedBy,
                $"Connected routing destination '{command.DestinationId}'.",
                command.DestinationId,
                "RoutingDestination",
                null,
                JsonSerializer.Serialize(new { command.DestinationId, command.VideoSourceId, command.AudioSourceId, command.AncillarySourceId })),
            cancellationToken);

        return ServiceResult.Success("Routing change submitted.");
    }

    public async Task<ServiceResult> DisconnectAsync(RoutingDisconnectCommand command, CancellationToken cancellationToken)
    {
        var topology = await topologyService.GetTopologyAsync(true, cancellationToken);
        var snapshot = new TopologySnapshotDto(
            topology.Registry,
            topology.Nodes,
            topology.Devices,
            topology.Sources,
            topology.Flows,
            topology.Senders,
            topology.Receivers,
            topology.RoutingDestinations,
            topology.RefreshedAtUtc);

        var destination = routingMatrixService.ResolveDestination(snapshot, command.DestinationId);
        var operations = new (bool Enabled, string? ReceiverId)[]
        {
            (command.DisconnectVideo, destination.VideoReceiverId),
            (command.DisconnectAudio, destination.AudioReceiverId),
            (command.DisconnectAncillary, destination.AncillaryReceiverId)
        };

        foreach (var operation in operations.Where(x => x.Enabled && x.ReceiverId is not null))
        {
            await connectionClient.ApplyConnectionAsync(
                new ConnectionRequest
                {
                    Operation = ConnectionOperation.Disconnect,
                    ReceiverId = operation.ReceiverId!,
                    Activation = command.Activation,
                    RequestedBy = command.RequestedBy,
                    RequestedAtUtc = DateTimeOffset.UtcNow
                },
                cancellationToken);
        }

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.ReceiverDisconnected,
                command.RequestedBy,
                $"Disconnected routing destination '{command.DestinationId}'.",
                command.DestinationId,
                "RoutingDestination",
                null,
                JsonSerializer.Serialize(new { command.DestinationId, command.DisconnectVideo, command.DisconnectAudio, command.DisconnectAncillary })),
            cancellationToken);

        return ServiceResult.Success("Routing disconnect submitted.");
    }
}
