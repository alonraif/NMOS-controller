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
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestStarted,
                command.RequestedBy,
                $"Route request started for receiver '{command.ReceiverId}'.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { command.ReceiverId, command.SenderId, command.Activation.Mode })),
            cancellationToken);

        var topology = await topologyService.GetTopologyAsync(true, cancellationToken);
        var sender = topology.Senders.FirstOrDefault(x => x.Id == command.SenderId);
        var receiver = topology.Receivers.FirstOrDefault(x => x.Id == command.ReceiverId);
        var previousSenderId = receiver?.Active.SenderId;

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

        if (assessment.Status == CompatibilityStatus.Incompatible)
        {
            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.ValidationFailedBlocking,
                    "system",
                    $"Blocking validation failure from sender '{command.SenderId}' to receiver '{command.ReceiverId}'.",
                    command.ReceiverId,
                    nameof(ResourceKind.Receiver),
                    null,
                    JsonSerializer.Serialize(new { command.SenderId, command.ReceiverId, assessment.Status })),
                cancellationToken);
            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.RouteRequestFailed,
                    command.RequestedBy,
                    $"Route request failed validation for receiver '{command.ReceiverId}'.",
                    command.ReceiverId,
                    nameof(ResourceKind.Receiver),
                    null,
                    JsonSerializer.Serialize(new { command.ReceiverId, command.SenderId, Reason = "ValidationFailed" })),
                cancellationToken);
            return ServiceResult.Failure("Connection request failed validation.");
        }

        await connectionClient.ApplyConnectionAsync(
            CreateConnectRequest(command.ReceiverId, command.SenderId, command.Activation, command.RequestedBy, sender, receiver),
            cancellationToken);
        topologyService.InvalidateSnapshot();

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
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.ReceiverStateChanged,
                command.RequestedBy,
                $"Receiver '{command.ReceiverId}' sender changed.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { ReceiverId = command.ReceiverId, PreviousSenderId = previousSenderId, NewSenderId = command.SenderId })),
            cancellationToken);
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestCompleted,
                command.RequestedBy,
                $"Route request completed for receiver '{command.ReceiverId}'.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { command.ReceiverId, command.SenderId })),
            cancellationToken);

        return ServiceResult.Success("Connection request submitted.");
    }

    public async Task<ServiceResult> DisconnectAsync(RouteDisconnectCommand command, CancellationToken cancellationToken)
    {
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestStarted,
                command.RequestedBy,
                $"Disconnect request started for receiver '{command.ReceiverId}'.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { command.ReceiverId, command.Activation.Mode })),
            cancellationToken);

        var topology = await topologyService.GetTopologyAsync(true, cancellationToken);
        var receiver = topology.Receivers.FirstOrDefault(x => x.Id == command.ReceiverId);
        var previousSenderId = receiver?.Active.SenderId;

        await connectionClient.ApplyConnectionAsync(
            new ConnectionRequest
            {
                Operation = ConnectionOperation.Disconnect,
                ReceiverId = command.ReceiverId,
                ConnectionApiBaseUrl = receiver?.ConnectionApiBaseUrl,
                Activation = command.Activation,
                RequestedBy = command.RequestedBy,
                RequestedAtUtc = DateTimeOffset.UtcNow
            },
            cancellationToken);
        topologyService.InvalidateSnapshot();

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
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.ReceiverStateChanged,
                command.RequestedBy,
                $"Receiver '{command.ReceiverId}' sender changed.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { ReceiverId = command.ReceiverId, PreviousSenderId = previousSenderId, NewSenderId = (string?)null })),
            cancellationToken);
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestCompleted,
                command.RequestedBy,
                $"Disconnect request completed for receiver '{command.ReceiverId}'.",
                command.ReceiverId,
                nameof(ResourceKind.Receiver),
                null,
                JsonSerializer.Serialize(new { command.ReceiverId })),
            cancellationToken);

        return ServiceResult.Success("Disconnect request submitted.");
    }

    public async Task<ServiceResult> ConnectAsync(RoutingConnectCommand command, CancellationToken cancellationToken)
    {
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestStarted,
                command.RequestedBy,
                $"Routing destination connect started for '{command.DestinationId}'.",
                command.DestinationId,
                "RoutingDestination",
                null,
                JsonSerializer.Serialize(new { command.DestinationId, command.VideoSourceId, command.AudioSourceId, command.AncillarySourceId })),
            cancellationToken);

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
            var sender = snapshot.Senders.FirstOrDefault(x => x.Id == senderId);
            var receiver = snapshot.Receivers.FirstOrDefault(x => x.Id == operation.ReceiverId!);
            var assessment = compatibilityEvaluator.Evaluate(
                sender?.ToDomain(),
                receiver?.ToDomain(),
                command.Activation);

            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.ConnectionValidated,
                    "system",
                    $"Validated route from sender '{senderId}' to receiver '{operation.ReceiverId}'.",
                    operation.ReceiverId!,
                    nameof(ResourceKind.Receiver),
                    null,
                    JsonSerializer.Serialize(new { SenderId = senderId, ReceiverId = operation.ReceiverId, assessment.Status })),
                cancellationToken);

            if (assessment.Status == CompatibilityStatus.Incompatible)
            {
                await auditService.RecordAsync(
                    new CreateAuditEntryCommand(
                        AuditActionType.ValidationFailedBlocking,
                        "system",
                        $"Blocking validation failure for routing destination '{command.DestinationId}' ({operation.Layer}).",
                        operation.ReceiverId!,
                        nameof(ResourceKind.Receiver),
                        null,
                        JsonSerializer.Serialize(new { command.DestinationId, operation.Layer, SenderId = senderId, ReceiverId = operation.ReceiverId })),
                    cancellationToken);
                await auditService.RecordAsync(
                    new CreateAuditEntryCommand(
                        AuditActionType.RouteRequestFailed,
                        command.RequestedBy,
                        $"Routing destination connect failed validation for '{command.DestinationId}'.",
                        command.DestinationId,
                        "RoutingDestination",
                        null,
                        JsonSerializer.Serialize(new { command.DestinationId, operation.Layer, SenderId = senderId, ReceiverId = operation.ReceiverId })),
                    cancellationToken);
                return ServiceResult.Failure($"Routing request for {operation.Layer} failed validation.");
            }

            await connectionClient.ApplyConnectionAsync(
                CreateConnectRequest(operation.ReceiverId!, senderId, command.Activation, command.RequestedBy, sender, receiver),
                cancellationToken);
            topologyService.InvalidateSnapshot();
            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.ReceiverStateChanged,
                    command.RequestedBy,
                    $"Receiver '{operation.ReceiverId}' sender changed.",
                    operation.ReceiverId!,
                    nameof(ResourceKind.Receiver),
                    null,
                    JsonSerializer.Serialize(new { ReceiverId = operation.ReceiverId, PreviousSenderId = receiver?.Active.SenderId, NewSenderId = senderId, operation.Layer })),
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
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestCompleted,
                command.RequestedBy,
                $"Routing destination connect completed for '{command.DestinationId}'.",
                command.DestinationId,
                "RoutingDestination",
                null,
                JsonSerializer.Serialize(new { command.DestinationId })),
            cancellationToken);

        return ServiceResult.Success("Routing change submitted.");
    }

    public async Task<ServiceResult> DisconnectAsync(RoutingDisconnectCommand command, CancellationToken cancellationToken)
    {
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestStarted,
                command.RequestedBy,
                $"Routing destination disconnect started for '{command.DestinationId}'.",
                command.DestinationId,
                "RoutingDestination",
                null,
                JsonSerializer.Serialize(new { command.DestinationId, command.DisconnectVideo, command.DisconnectAudio, command.DisconnectAncillary })),
            cancellationToken);

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
            var receiver = snapshot.Receivers.FirstOrDefault(x => x.Id == operation.ReceiverId);
            var previousSenderId = receiver?.Active.SenderId;
            await connectionClient.ApplyConnectionAsync(
                new ConnectionRequest
                {
                    Operation = ConnectionOperation.Disconnect,
                    ReceiverId = operation.ReceiverId!,
                    ConnectionApiBaseUrl = receiver?.ConnectionApiBaseUrl,
                    Activation = command.Activation,
                    RequestedBy = command.RequestedBy,
                    RequestedAtUtc = DateTimeOffset.UtcNow
                },
                cancellationToken);
            topologyService.InvalidateSnapshot();
            await auditService.RecordAsync(
                new CreateAuditEntryCommand(
                    AuditActionType.ReceiverStateChanged,
                    command.RequestedBy,
                    $"Receiver '{operation.ReceiverId}' sender changed.",
                    operation.ReceiverId!,
                    nameof(ResourceKind.Receiver),
                    null,
                    JsonSerializer.Serialize(new { ReceiverId = operation.ReceiverId, PreviousSenderId = previousSenderId, NewSenderId = (string?)null })),
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
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.RouteRequestCompleted,
                command.RequestedBy,
                $"Routing destination disconnect completed for '{command.DestinationId}'.",
                command.DestinationId,
                "RoutingDestination",
                null,
                JsonSerializer.Serialize(new { command.DestinationId })),
            cancellationToken);

        return ServiceResult.Success("Routing disconnect submitted.");
    }

    private static ConnectionRequest CreateConnectRequest(
        string receiverId,
        string senderId,
        Domain.ValueObjects.ActivationRequest activation,
        string requestedBy,
        NmosSenderDto? sender,
        NmosReceiverDto? receiver)
    {
        var transportParameters = SelectTransportParameters(receiver);

        return new ConnectionRequest
        {
            Operation = ConnectionOperation.Connect,
            ReceiverId = receiverId,
            SenderId = senderId,
            TransportFile = sender?.TransportFile,
            TransportParameters = transportParameters.Count == 0
                ? Array.Empty<IReadOnlyDictionary<string, string>>()
                : new[] { transportParameters },
            ConnectionApiBaseUrl = receiver?.ConnectionApiBaseUrl,
            Activation = activation,
            RequestedBy = requestedBy,
            RequestedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static IReadOnlyDictionary<string, string> SelectTransportParameters(NmosReceiverDto? receiver)
    {
        if (receiver is null)
        {
            return new Dictionary<string, string>();
        }

        if (receiver.Staged.TransportParameters.Count > 0)
        {
            return receiver.Staged.TransportParameters;
        }

        if (receiver.Active.TransportParameters.Count > 0)
        {
            return receiver.Active.TransportParameters;
        }

        return new Dictionary<string, string>();
    }
}
