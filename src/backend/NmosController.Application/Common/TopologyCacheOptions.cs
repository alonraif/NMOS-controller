namespace NmosController.Application.Common;

public sealed class TopologyCacheOptions
{
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromSeconds(5);
}
