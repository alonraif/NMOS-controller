using NmosController.Domain.Enums;

namespace NmosController.Application.Topology;

public sealed record RegistrySummaryDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string QueryApiVersion,
    string ConnectionApiVersion,
    ControllerMode Mode,
    bool IsEnabled);
