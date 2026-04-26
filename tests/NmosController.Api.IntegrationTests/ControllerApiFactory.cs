using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Application.Common;
using NmosController.Application.Presets;
using NmosController.Application.Routing;
using NmosController.Application.Settings;
using NmosController.Application.Topology;
using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;

namespace NmosController.Api.IntegrationTests;

public sealed class ControllerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITopologyService>();
            services.RemoveAll<IRegistryService>();
            services.RemoveAll<IRoutingService>();
            services.RemoveAll<IPresetService>();
            services.RemoveAll<IAuditService>();

            services.AddSingleton<ITopologyService, FakeTopologyService>();
            services.AddSingleton<IRegistryService, FakeRegistryService>();
            services.AddSingleton<IRoutingService, FakeRoutingService>();
            services.AddSingleton<IPresetService, FakePresetService>();
            services.AddSingleton<IAuditService, FakeAuditService>();
        });
    }

    private sealed class FakeTopologyService : ITopologyService
    {
        public Task<TopologyGraphDto> GetTopologyAsync(bool forceRefresh, CancellationToken cancellationToken) =>
            Task.FromResult(
                new TopologyGraphDto(
                    new RegistrySummaryDto(Guid.Empty, "Mock Registry", "http://mock", "v1.3", "v1.1", ControllerMode.Mock, true),
                    Array.Empty<NmosNodeDto>(),
                    Array.Empty<NmosDeviceDto>(),
                    Array.Empty<NmosSourceDto>(),
                    Array.Empty<NmosFlowDto>(),
                    [
                        new NmosSenderDto(
                            "sender-a",
                            "node-a",
                            "device-a",
                            null,
                            "Sender A",
                            NmosTransportType.Rtp,
                            new MediaFormatSummary("urn:x-nmos:format:audio", "audio/L24", null, null, null, "48000/1"),
                            null,
                            null,
                            new TransportFileData("application/sdp", "v=0"),
                            "Audio",
                            "source-a",
                            "Sender A",
                            null,
                            "A",
                            true,
                            DateTimeOffset.UtcNow)
                    ],
                    [
                        new NmosReceiverDto(
                            "receiver-a",
                            "node-a",
                            "device-a",
                            "Receiver A",
                            NmosTransportType.Rtp,
                            new MediaFormatSummary("urn:x-nmos:format:audio", "audio/L24", null, null, null, "48000/1"),
                            new ConstraintSet(Array.Empty<ConstraintParameter>(), ["audio/L24"], [NmosTransportType.Rtp], true),
                            new ConnectionState(null, "false", new Dictionary<string, string>(), null),
                            new ConnectionState(null, "false", new Dictionary<string, string>(), null),
                            true,
                            "Audio",
                            "receiver-a",
                            "Receiver A",
                            DateTimeOffset.UtcNow)
                    ],
                    [new RoutingDestinationSnapshotDto("receiver-a", "Receiver A", "node-a", "device-a", null, "receiver-a", null, Array.Empty<string>())],
                    Array.Empty<TopologyRouteEdgeDto>(),
                    DateTimeOffset.UtcNow));

        public Task<IReadOnlyCollection<NmosSenderDto>> GetSendersAsync(bool forceRefresh, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<NmosSenderDto>>(Array.Empty<NmosSenderDto>());

        public Task<IReadOnlyCollection<NmosReceiverDto>> GetReceiversAsync(bool forceRefresh, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<NmosReceiverDto>>(Array.Empty<NmosReceiverDto>());

        public Task<ResourceDetailDto?> GetResourceAsync(string resourceId, CancellationToken cancellationToken) =>
            Task.FromResult<ResourceDetailDto?>(null);
    }

    private sealed class FakeRegistryService : IRegistryService
    {
        public Task<RegistrySettingsDto?> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult<RegistrySettingsDto?>(
                new RegistrySettingsDto(Guid.Empty, "Mock Registry", "http://mock", null, null, "v1.3", "v1.1", ControllerMode.Mock, true, DateTimeOffset.UtcNow));

        public Task<RegistrySettingsDto> SaveAsync(UpdateRegistrySettingsCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(
                new RegistrySettingsDto(Guid.Empty, command.Name, command.BaseUrl, command.ConnectionBaseUrl, command.ConnectionBaseUrls, command.QueryApiVersion, command.ConnectionApiVersion, command.Mode, command.IsEnabled, DateTimeOffset.UtcNow));
    }

    private sealed class FakeRoutingService : IRoutingService
    {
        public Task<RoutingMatrixDto> GetMatrixAsync(bool forceRefresh, CancellationToken cancellationToken) =>
            Task.FromResult(new RoutingMatrixDto(Array.Empty<RoutingSourceDto>(), Array.Empty<RoutingDestinationDto>(), Array.Empty<RoutingCrosspointDto>(), DateTimeOffset.UtcNow));

        public Task<RouteValidationResultDto> ValidateAsync(RouteValidationCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(new RouteValidationResultDto(CompatibilityStatus.Compatible, Array.Empty<RouteValidationIssueDto>()));

        public Task<ServiceResult> ConnectAsync(RouteConnectCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(ServiceResult.Success("connected"));

        public Task<ServiceResult> DisconnectAsync(RouteDisconnectCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(ServiceResult.Success("disconnected"));

        public Task<ServiceResult> ConnectAsync(RoutingConnectCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(ServiceResult.Success("connected"));

        public Task<ServiceResult> DisconnectAsync(RoutingDisconnectCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(ServiceResult.Success("disconnected"));
    }

    private sealed class FakePresetService : IPresetService
    {
        public Task<IReadOnlyCollection<PresetSalvoDto>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<PresetSalvoDto>>(Array.Empty<PresetSalvoDto>());

        public Task<PresetSalvoDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<PresetSalvoDto?>(null);

        public Task<Guid> SaveAsync(UpsertPresetSalvoCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(ServiceResult.Success("deleted"));

        public Task<ServiceResult> ExecuteAsync(ExecutePresetSalvoCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(ServiceResult.Success("executed"));
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task<IReadOnlyCollection<AuditEntryDto>> GetRecentAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<AuditEntryDto>>(
                [
                    new AuditEntryDto(Guid.NewGuid(), AuditActionType.ConnectionValidated, "tester", "Validated route.", "receiver-a", "Receiver", "trace", DateTimeOffset.UtcNow, null)
                ]);

        public Task RecordAsync(CreateAuditEntryCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
