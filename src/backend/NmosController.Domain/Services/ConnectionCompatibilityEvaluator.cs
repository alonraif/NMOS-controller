using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Domain.Services;

public sealed class ConnectionCompatibilityEvaluator
{
    public CompatibilityAssessment Evaluate(NmosSender? sender, NmosReceiver? receiver, ActivationRequest activation)
    {
        var issues = new List<CompatibilityIssue>();

        if (sender is null)
        {
            issues.Add(new CompatibilityIssue("sender.missing", "Sender was not found.", true));
        }

        if (receiver is null)
        {
            issues.Add(new CompatibilityIssue("receiver.missing", "Receiver was not found.", true));
        }

        if (sender is null || receiver is null)
        {
            return new CompatibilityAssessment(CompatibilityStatus.Incompatible, issues);
        }

        if (!receiver.IsConnectable)
        {
            issues.Add(new CompatibilityIssue("receiver.not_connectable", "Receiver is not currently connectable.", true));
        }

        if (sender.Transport != receiver.Transport)
        {
            issues.Add(
                new CompatibilityIssue(
                    "transport.mismatch",
                    $"Sender transport '{sender.Transport}' does not match receiver transport '{receiver.Transport}'.",
                    true));
        }

        if (!string.Equals(sender.Format.Format, receiver.Format.Format, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(
                new CompatibilityIssue(
                    "format.mismatch",
                    $"Sender format '{sender.Format.Format}' does not match receiver format '{receiver.Format.Format}'.",
                    true));
        }

        if (receiver.Constraints.TransportTypes.Count > 0 && !receiver.Constraints.TransportTypes.Contains(sender.Transport))
        {
            issues.Add(new CompatibilityIssue("receiver.constraints.transport", "Receiver transport constraints exclude this sender transport.", true));
        }

        if (receiver.Constraints.MediaTypes.Count > 0 &&
            !string.IsNullOrWhiteSpace(sender.Format.MediaType) &&
            !receiver.Constraints.MediaTypes.Contains(sender.Format.MediaType, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(new CompatibilityIssue("receiver.constraints.media_type", "Receiver media type constraints exclude this sender media type.", true));
        }

        if (receiver.Constraints.RequiresTransportFile && sender.TransportFile is null)
        {
            issues.Add(new CompatibilityIssue("transport_file.required", "Receiver requires an SDP or transport file, but the sender does not expose one.", true));
        }

        if (activation.Mode != ActivationModeType.Immediate &&
            activation.ActivationTimeUtc is null &&
            activation.RequestedOffset is null)
        {
            issues.Add(new CompatibilityIssue("activation.invalid", "Scheduled activation requires either an absolute time or relative offset.", true));
        }

        if (issues.Count == 0)
        {
            return CompatibilityAssessment.Compatible();
        }

        var status = issues.Any(issue => issue.IsBlocking)
            ? CompatibilityStatus.Incompatible
            : CompatibilityStatus.Warning;

        return new CompatibilityAssessment(status, issues);
    }
}
