namespace NmosController.Application.Topology;

public sealed record RegistrySummaryDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string QueryApiVersion,
    string ConnectionApiVersion,
    bool IsEnabled);
