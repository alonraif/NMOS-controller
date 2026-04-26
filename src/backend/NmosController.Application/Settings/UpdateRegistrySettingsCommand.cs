using NmosController.Domain.Enums;

namespace NmosController.Application.Settings;

public sealed record UpdateRegistrySettingsCommand(
    string Name,
    string BaseUrl,
    string? ConnectionBaseUrl,
    string? ConnectionBaseUrls,
    string QueryApiVersion,
    string ConnectionApiVersion,
    ControllerMode Mode,
    bool IsEnabled);
