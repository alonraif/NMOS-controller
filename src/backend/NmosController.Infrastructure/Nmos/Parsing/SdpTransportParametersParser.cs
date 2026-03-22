using System.Text.RegularExpressions;
using NmosController.Domain.ValueObjects;

namespace NmosController.Infrastructure.Nmos.Parsing;

internal static partial class SdpTransportParametersParser
{
    public static IReadOnlyCollection<Dictionary<string, object?>>? BuildPrimaryLeg(
        TransportFileData? transportFile,
        string interfaceIp)
    {
        if (transportFile is null || string.IsNullOrWhiteSpace(transportFile.Content))
        {
            return null;
        }

        var normalized = transportFile.Content.Replace("\r\n", "\n");
        var mediaMatch = MediaDescriptionRegex().Match(normalized);
        var connectionMatch = ConnectionDataRegex().Match(normalized);

        if (!mediaMatch.Success || !connectionMatch.Success)
        {
            return null;
        }

        return
        [
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["destination_port"] = int.Parse(mediaMatch.Groups["port"].Value),
                ["interface_ip"] = interfaceIp,
                ["multicast_ip"] = connectionMatch.Groups["ip"].Value,
                ["source_ip"] = "0.0.0.0",
                ["rtp_enabled"] = true
            }
        ];
    }

    [GeneratedRegex(@"^m=\w+\s+(?<port>\d+)\s+", RegexOptions.Multiline)]
    private static partial Regex MediaDescriptionRegex();

    [GeneratedRegex(@"^c=IN\s+IP4\s+(?<ip>\d{1,3}(?:\.\d{1,3}){3})", RegexOptions.Multiline)]
    private static partial Regex ConnectionDataRegex();
}
