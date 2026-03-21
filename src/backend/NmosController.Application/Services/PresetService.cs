using System.Text.Json;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Application.Common;
using NmosController.Application.Mappers;
using NmosController.Application.Presets;
using NmosController.Application.Routing;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;

namespace NmosController.Application.Services;

public sealed class PresetService(
    IPresetSalvoRepository presetRepository,
    IRoutingService routingService,
    IAuditService auditService) : IPresetService
{
    public async Task<IReadOnlyCollection<PresetSalvoDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var presets = await presetRepository.GetAllAsync(cancellationToken);
        return presets.Select(x => x.ToDto()).ToArray();
    }

    public async Task<PresetSalvoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var preset = await presetRepository.GetByIdAsync(id, cancellationToken);
        return preset?.ToDto();
    }

    public async Task<Guid> SaveAsync(UpsertPresetSalvoCommand command, CancellationToken cancellationToken)
    {
        var preset = command.Id.HasValue
            ? await presetRepository.GetByIdAsync(command.Id.Value, cancellationToken) ?? new PresetSalvo { Id = command.Id.Value }
            : new PresetSalvo();

        preset.UpdateDetails(command.Name, command.Description, command.Routes, DateTimeOffset.UtcNow);

        if (command.Id.HasValue)
        {
            await presetRepository.UpdateAsync(preset, cancellationToken);
        }
        else
        {
            await presetRepository.AddAsync(preset, cancellationToken);
        }

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                command.Id.HasValue ? AuditActionType.PresetUpdated : AuditActionType.PresetCreated,
                "operator",
                $"{(command.Id.HasValue ? "Updated" : "Created")} preset '{preset.Name}'.",
                preset.Id.ToString(),
                nameof(PresetSalvo),
                null,
                JsonSerializer.Serialize(new { preset.Name, RouteCount = preset.Routes.Count })),
            cancellationToken);

        return preset.Id;
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var existing = await presetRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return ServiceResult.Failure("Preset was not found.");
        }

        await presetRepository.DeleteAsync(id, cancellationToken);
        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.PresetDeleted,
                "operator",
                $"Deleted preset '{existing.Name}'.",
                id.ToString(),
                nameof(PresetSalvo),
                null,
                null),
            cancellationToken);

        return ServiceResult.Success("Preset deleted.");
    }

    public async Task<ServiceResult> ExecuteAsync(ExecutePresetSalvoCommand command, CancellationToken cancellationToken)
    {
        var preset = await presetRepository.GetByIdAsync(command.PresetId, cancellationToken);
        if (preset is null)
        {
            return ServiceResult.Failure("Preset was not found.");
        }

        foreach (var route in preset.Routes)
        {
            var activation = command.OverrideActivation ?? route.Activation;
            if (string.IsNullOrWhiteSpace(route.SenderId))
            {
                var disconnect = await routingService.DisconnectAsync(
                    new RouteDisconnectCommand(route.ReceiverId, command.RequestedBy, activation),
                    cancellationToken);

                if (!disconnect.Succeeded)
                {
                    return disconnect;
                }
            }
            else
            {
                var connect = await routingService.ConnectAsync(
                    new RouteConnectCommand(route.ReceiverId, route.SenderId, command.RequestedBy, activation),
                    cancellationToken);

                if (!connect.Succeeded)
                {
                    return connect;
                }
            }
        }

        await auditService.RecordAsync(
            new CreateAuditEntryCommand(
                AuditActionType.PresetExecuted,
                command.RequestedBy,
                $"Executed preset '{preset.Name}'.",
                preset.Id.ToString(),
                nameof(PresetSalvo),
                null,
                JsonSerializer.Serialize(new { preset.Name, RouteCount = preset.Routes.Count })),
            cancellationToken);

        return ServiceResult.Success("Preset execution submitted.");
    }
}
