using NmosController.Domain.Enums;

namespace NmosController.Domain.ValueObjects;

public sealed record ConstraintSet(
    IReadOnlyCollection<ConstraintParameter> Parameters,
    IReadOnlyCollection<string> MediaTypes,
    IReadOnlyCollection<NmosTransportType> TransportTypes,
    bool RequiresTransportFile)
{
    public static ConstraintSet Empty { get; } =
        new(
            Array.Empty<ConstraintParameter>(),
            Array.Empty<string>(),
            Array.Empty<NmosTransportType>(),
            false);
}
