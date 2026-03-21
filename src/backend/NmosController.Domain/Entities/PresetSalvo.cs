using NmosController.Domain.ValueObjects;

namespace NmosController.Domain.Entities;

public sealed class PresetSalvo
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public IReadOnlyCollection<PresetRoute> Routes { get; private set; } = Array.Empty<PresetRoute>();
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public void UpdateDetails(string name, string? description, IReadOnlyCollection<PresetRoute> routes, DateTimeOffset updatedAtUtc)
    {
        Name = name;
        Description = description;
        Routes = routes;
        UpdatedAtUtc = updatedAtUtc;
    }
}
