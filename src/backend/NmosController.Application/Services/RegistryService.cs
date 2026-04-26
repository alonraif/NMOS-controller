using NmosController.Application.Abstractions.Persistence;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Mappers;
using NmosController.Application.Settings;
using NmosController.Domain.Entities;

namespace NmosController.Application.Services;

public sealed class RegistryService(IRegistryRepository registryRepository) : IRegistryService
{
    public async Task<RegistrySettingsDto?> GetAsync(CancellationToken cancellationToken)
    {
        var registry = await registryRepository.GetPrimaryAsync(cancellationToken);
        return registry?.ToDto();
    }

    public async Task<RegistrySettingsDto> SaveAsync(UpdateRegistrySettingsCommand command, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(command.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("Registry base URL is not a valid absolute URI.");
        }

        var normalizedConnectionBaseUrl = NormalizeConnectionBaseUrl(command.ConnectionBaseUrl);
        var normalizedConnectionBaseUrls = NormalizeConnectionBaseUrls(command.ConnectionBaseUrls);

        var existing = await registryRepository.GetPrimaryAsync(cancellationToken);
        var registry = existing ?? new Registry();
        registry.Update(
            command.Name,
            baseUri,
            command.QueryApiVersion,
            command.ConnectionApiVersion,
            command.IsEnabled,
            DateTimeOffset.UtcNow,
            normalizedConnectionBaseUrl,
            normalizedConnectionBaseUrls);

        await registryRepository.SaveAsync(registry, cancellationToken);
        return registry.ToDto();
    }

    private static string? NormalizeConnectionBaseUrl(string? rawConnectionBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(rawConnectionBaseUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(rawConnectionBaseUrl, UriKind.Absolute, out var parsed))
        {
            throw new InvalidOperationException("Registry connection base URL is not a valid absolute URI.");
        }

        return $"{parsed.Scheme}://{parsed.Authority}";
    }

    private static string? NormalizeConnectionBaseUrls(string? rawConnectionBaseUrls)
    {
        if (string.IsNullOrWhiteSpace(rawConnectionBaseUrls))
        {
            return null;
        }

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in rawConnectionBaseUrls.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var parsed))
            {
                throw new InvalidOperationException($"Registry connection base URL '{candidate}' is not a valid absolute URI.");
            }

            var normalizedCandidate = $"{parsed.Scheme}://{parsed.Authority}";
            if (seen.Add(normalizedCandidate))
            {
                normalized.Add(normalizedCandidate);
            }
        }

        return normalized.Count == 0 ? null : string.Join(",", normalized);
    }
}
