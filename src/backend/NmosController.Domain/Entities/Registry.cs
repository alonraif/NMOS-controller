namespace NmosController.Domain.Entities;

public sealed class Registry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public Uri BaseUrl { get; private set; } = new("http://localhost");
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
