namespace NmosController.Infrastructure.Configuration;

internal interface IMdnsRegistryDiscovery
{
    Task<MdnsDiscoveryResult?> DiscoverAsync(string serviceType, TimeSpan timeout, CancellationToken cancellationToken);
}
