using Microsoft.Extensions.Logging;
using Zeroconf;

namespace NmosController.Infrastructure.Configuration;

internal sealed class MdnsRegistryDiscovery(ILogger<MdnsRegistryDiscovery> logger) : IMdnsRegistryDiscovery
{
    public async Task<MdnsDiscoveryResult?> DiscoverAsync(string serviceType, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        IReadOnlyList<IZeroconfHost> hosts;
        try
        {
            hosts = await ZeroconfResolver.ResolveAsync(serviceType, cancellationToken: timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("mDNS discovery timed out for service type {ServiceType}.", serviceType);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "mDNS discovery failed for service type {ServiceType}.", serviceType);
            return null;
        }

        var candidates = new List<(Uri BaseUrl, string? ApiVersion)>();
        foreach (var host in hosts)
        {
            if (!host.Services.TryGetValue(serviceType, out var service))
            {
                continue;
            }

            if (service.Port <= 0)
            {
                continue;
            }

            var hostAddress = string.IsNullOrWhiteSpace(host.IPAddress) ? host.DisplayName : host.IPAddress;
            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                continue;
            }

            if (!Uri.TryCreate($"http://{hostAddress}:{service.Port}", UriKind.Absolute, out var baseUrl))
            {
                continue;
            }

            candidates.Add((baseUrl, ParseApiVersion(service)));
        }

        if (candidates.Count == 0)
        {
            logger.LogInformation("mDNS did not return valid registry candidates for {ServiceType}.", serviceType);
            return null;
        }

        var selected = candidates
            .OrderByDescending(x => ExtractVersionNumber(x.ApiVersion))
            .ThenBy(x => x.BaseUrl.Host, StringComparer.OrdinalIgnoreCase)
            .First();

        return new MdnsDiscoveryResult(selected.BaseUrl, selected.ApiVersion, DateTimeOffset.UtcNow);
    }

    private static string? ParseApiVersion(IService service)
    {
        var txtRecordsProperty = service.GetType().GetProperty("TxtRecords");
        var txtRecords = txtRecordsProperty?.GetValue(service) as System.Collections.IEnumerable;
        if (txtRecords is null)
        {
            return null;
        }

        var versions = new List<string>();
        foreach (var record in txtRecords)
        {
            if (record is System.Collections.IEnumerable tupleList)
            {
                foreach (var pair in tupleList)
                {
                    if (pair is KeyValuePair<string, string> stringPair
                        && string.Equals(stringPair.Key, "api_ver", StringComparison.OrdinalIgnoreCase))
                    {
                        versions.AddRange(stringPair.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                    }

                    if (pair is KeyValuePair<string, object> objectPair
                        && string.Equals(objectPair.Key, "api_ver", StringComparison.OrdinalIgnoreCase))
                    {
                        versions.AddRange(objectPair.Value?.ToString()?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) ?? []);
                    }
                }
            }
        }

        return versions.OrderByDescending(ExtractVersionNumber).FirstOrDefault();
    }

    private static decimal ExtractVersionNumber(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return 0;
        }

        var normalized = version.Trim().TrimStart('v', 'V');
        return decimal.TryParse(normalized, out var parsed) ? parsed : 0;
    }
}
