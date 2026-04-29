namespace NmosController.Infrastructure.Configuration;

internal sealed record MdnsDiscoveryResult(
    Uri QueryBaseUrl,
    string? QueryApiVersion,
    DateTimeOffset DiscoveredAtUtc);
