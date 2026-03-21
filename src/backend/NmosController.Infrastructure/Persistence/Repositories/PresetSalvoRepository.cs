using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NmosController.Application.Abstractions.Persistence;
using NmosController.Domain.Entities;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Json;
using NmosController.Infrastructure.Persistence.Entities;

namespace NmosController.Infrastructure.Persistence.Repositories;

public sealed class PresetSalvoRepository(ControllerDbContext dbContext) : IPresetSalvoRepository
{
    public async Task<IReadOnlyCollection<PresetSalvo>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext.Presets
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(Map).ToArray();
    }

    public async Task<PresetSalvo?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Presets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task AddAsync(PresetSalvo preset, CancellationToken cancellationToken)
    {
        dbContext.Presets.Add(ToEntity(preset));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PresetSalvo preset, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Presets.SingleAsync(x => x.Id == preset.Id, cancellationToken);
        entity.Name = preset.Name;
        entity.Description = preset.Description;
        entity.RoutesJson = JsonSerializer.Serialize(preset.Routes, NmosJsonSerializer.Default);
        entity.UpdatedAtUtc = preset.UpdatedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Presets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        dbContext.Presets.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PresetSalvoEntity ToEntity(PresetSalvo preset) =>
        new()
        {
            Id = preset.Id,
            Name = preset.Name,
            Description = preset.Description,
            RoutesJson = JsonSerializer.Serialize(preset.Routes, NmosJsonSerializer.Default),
            CreatedAtUtc = preset.CreatedAtUtc,
            UpdatedAtUtc = preset.UpdatedAtUtc
        };

    private static PresetSalvo Map(PresetSalvoEntity entity)
    {
        var routes = JsonSerializer.Deserialize<IReadOnlyCollection<PresetRoute>>(entity.RoutesJson, NmosJsonSerializer.Default)
            ?? Array.Empty<PresetRoute>();

        var preset = new PresetSalvo
        {
            Id = entity.Id,
            CreatedAtUtc = entity.CreatedAtUtc
        };

        preset.UpdateDetails(entity.Name, entity.Description, routes, entity.UpdatedAtUtc);
        return preset;
    }
}
