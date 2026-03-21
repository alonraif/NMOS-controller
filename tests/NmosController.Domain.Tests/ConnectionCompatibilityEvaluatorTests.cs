using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Domain.Services;
using NmosController.Domain.ValueObjects;

namespace NmosController.Domain.Tests;

public sealed class ConnectionCompatibilityEvaluatorTests
{
    private readonly ConnectionCompatibilityEvaluator _sut = new();

    [Fact]
    public void Evaluate_WhenSenderAndReceiverAreCompatible_ReturnsCompatible()
    {
        var sender = CreateSender();
        var receiver = CreateReceiver();

        var result = _sut.Evaluate(sender, receiver, ActivationRequest.Immediate(DateTimeOffset.UtcNow));

        Assert.Equal(CompatibilityStatus.Compatible, result.Status);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Evaluate_WhenTransportDiffers_ReturnsIncompatible()
    {
        var sender = CreateSender() with { Transport = NmosTransportType.Dash };
        var receiver = CreateReceiver();

        var result = _sut.Evaluate(sender, receiver, ActivationRequest.Immediate(DateTimeOffset.UtcNow));

        Assert.Equal(CompatibilityStatus.Incompatible, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == "transport.mismatch");
    }

    [Fact]
    public void Evaluate_WhenTransportFileRequiredButMissing_ReturnsIncompatible()
    {
        var sender = CreateSender() with { TransportFile = null };
        var receiver = CreateReceiver();

        var result = _sut.Evaluate(sender, receiver, ActivationRequest.Immediate(DateTimeOffset.UtcNow));

        Assert.Equal(CompatibilityStatus.Incompatible, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == "transport_file.required");
    }

    private static NmosSender CreateSender() =>
        new()
        {
            Id = "sender-a",
            Label = "Sender A",
            Transport = NmosTransportType.Rtp,
            Format = new MediaFormatSummary("urn:x-nmos:format:audio", "audio/L24", null, null, null, "48000/1"),
            TransportFile = new TransportFileData("application/sdp", "v=0")
        };

    private static NmosReceiver CreateReceiver() =>
        new()
        {
            Id = "receiver-a",
            Label = "Receiver A",
            Transport = NmosTransportType.Rtp,
            Format = new MediaFormatSummary("urn:x-nmos:format:audio", "audio/L24", null, null, null, "48000/1"),
            Constraints = new ConstraintSet(
                Array.Empty<ConstraintParameter>(),
                ["audio/L24"],
                [NmosTransportType.Rtp],
                true),
            Active = new ConnectionState(null, "false", new Dictionary<string, string>(), null),
            Staged = new ConnectionState(null, "false", new Dictionary<string, string>(), null)
        };
}
