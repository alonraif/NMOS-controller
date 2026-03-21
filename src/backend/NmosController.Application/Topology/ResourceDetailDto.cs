using NmosController.Domain.Enums;

namespace NmosController.Application.Topology;

public sealed record ResourceDetailDto(
    string Id,
    ResourceKind Kind,
    object Payload);
