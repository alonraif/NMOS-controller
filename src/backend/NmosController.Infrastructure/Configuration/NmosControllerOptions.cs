using NmosController.Domain.Enums;

namespace NmosController.Infrastructure.Configuration;

public sealed class NmosControllerOptions
{
    public const string SectionName = "NMOS_CONTROLLER";

    public ControllerMode Mode { get; set; } = ControllerMode.Mock;
    public RegistryOptions Registry { get; set; } = new();
    public PostgresOptions Postgres { get; set; } = new();
    public NmosHttpOptions Http { get; set; } = new();
    public MockLabOptions MockLab { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
}

public sealed class RegistryOptions
{
    public string Name { get; set; } = "Local Mock Registry";
    public string BaseUrl { get; set; } = "http://localhost:8081";
    public string QueryApiVersion { get; set; } = "v1.3";
    public string ConnectionApiVersion { get; set; } = "v1.1";
    public bool IsEnabled { get; set; } = true;
}

public sealed class PostgresOptions
{
    public string ConnectionString { get; set; } =
        "Host=localhost;Port=5432;Database=nmos_controller;Username=nmos;Password=nmos";
}

public sealed class NmosHttpOptions
{
    public int TimeoutSeconds { get; set; } = 10;
    public int RetryAttempts { get; set; } = 3;
}

public sealed class MockLabOptions
{
    public string FixturePath { get; set; } = "Nmos/Mock/Fixtures/topology-snapshot.json";
}

public sealed class CorsOptions
{
    public string AllowedOrigins { get; set; } = "http://localhost:5173,http://localhost:8088,http://localhost:8080";
}
