namespace NmosController.Domain.ValueObjects;

public sealed record ConnectionState(
    string? SenderId,
    string? MasterEnable,
    IReadOnlyDictionary<string, string> TransportParameters,
    TransportFileData? TransportFile);

