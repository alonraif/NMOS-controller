namespace NmosController.Domain.Entities;

public sealed class Registry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public Uri BaseUrl { get; private set; } = new("http://localhost");
    public string DiscoveryMode { get; private set; } = "Manual";
    public string MdnsQueryServiceType { get; private set; } = "_nmos-query._tcp.local.";
    public int MdnsResolveTimeoutMilliseconds { get; private set; } = 2000;
    public string? ConnectionBaseUrl { get; private set; }
    public string? ConnectionBaseUrls { get; private set; }
    public string QueryApiVersion { get; private set; } = "v1.3";
    public string ConnectionApiVersion { get; private set; } = "v1.1";
    public bool IsEnabled { get; private set; } = true;
    public bool InitialSetupCompleted { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public void Update(
        string name,
        Uri baseUrl,
        string discoveryMode,
        string mdnsQueryServiceType,
        int mdnsResolveTimeoutMilliseconds,
        string queryApiVersion,
        string connectionApiVersion,
        bool isEnabled,
        DateTimeOffset updatedAtUtc,
        string? connectionBaseUrl = null,
        string? connectionBaseUrls = null,
        bool? initialSetupCompleted = null)
    {
        Name = name;
        BaseUrl = baseUrl;
        DiscoveryMode = discoveryMode;
        MdnsQueryServiceType = mdnsQueryServiceType;
        MdnsResolveTimeoutMilliseconds = mdnsResolveTimeoutMilliseconds;
        ConnectionBaseUrl = connectionBaseUrl;
        ConnectionBaseUrls = connectionBaseUrls;
        QueryApiVersion = queryApiVersion;
        ConnectionApiVersion = connectionApiVersion;
        IsEnabled = isEnabled;
        if (initialSetupCompleted.HasValue)
        {
            InitialSetupCompleted = initialSetupCompleted.Value;
        }
        UpdatedAtUtc = updatedAtUtc;
    }
}
