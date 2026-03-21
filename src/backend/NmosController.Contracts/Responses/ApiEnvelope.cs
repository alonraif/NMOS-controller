namespace NmosController.Contracts.Responses;

public sealed record ApiEnvelope<T>(
    T Data,
    DateTimeOffset Utc);
