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
    ConnectionCompatibilityEvaluator compatibilityEvaluator) : IRoutingService
{
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
}
