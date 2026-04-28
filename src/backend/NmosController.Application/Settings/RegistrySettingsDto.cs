namespace NmosController.Application.Settings;

public sealed record RegistrySettingsDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string? ConnectionBaseUrl,
    string? ConnectionBaseUrls,
    string QueryApiVersion,
    string ConnectionApiVersion,
    bool IsEnabled,
    bool InitialSetupCompleted,
    DateTimeOffset UpdatedAtUtc);
