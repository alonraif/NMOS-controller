using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Domain.Entities;

public sealed class ConnectionRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ConnectionOperation Operation { get; init; }
    public string ReceiverId { get; init; } = string.Empty;
    public string? SenderId { get; init; }
    public IReadOnlyCollection<IReadOnlyDictionary<string, string>> TransportParameters { get; init; } =
        Array.Empty<IReadOnlyDictionary<string, string>>();
    public TransportFileData? TransportFile { get; init; }
    public ActivationRequest Activation { get; init; } = ActivationRequest.Immediate(DateTimeOffset.UtcNow);
    public string RequestedBy { get; init; } = "system";
    public DateTimeOffset RequestedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
