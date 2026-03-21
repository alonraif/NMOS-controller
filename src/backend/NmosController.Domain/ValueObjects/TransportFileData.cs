namespace NmosController.Domain.ValueObjects;

public sealed record TransportFileData(
    string ContentType,
    string Content);
