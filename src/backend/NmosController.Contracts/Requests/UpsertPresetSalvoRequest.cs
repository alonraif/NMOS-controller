using System.ComponentModel.DataAnnotations;

namespace NmosController.Contracts.Requests;

public sealed class UpsertPresetSalvoRequest
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? Description { get; set; }

    [MinLength(1)]
    public List<PresetRouteRequest> Routes { get; set; } = [];
}
