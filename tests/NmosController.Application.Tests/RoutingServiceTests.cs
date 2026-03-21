using NmosController.Application.Abstractions.Integrations;
using NmosController.Application.Abstractions.Services;
using NmosController.Application.Audit;
using NmosController.Application.Common;
using NmosController.Application.Routing;
using NmosController.Application.Services;
using NmosController.Application.Topology;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Domain.Services;
using NmosController.Domain.ValueObjects;

namespace NmosController.Application.Tests;

public sealed class RoutingServiceTests
{
    [Fact]
    public async Task ConnectAsync_WhenValidationPasses_AppliesConnection()
    {
        var connectionClient = new RecordingConnectionClient();
        var auditService = new RecordingAuditService();
        var topologyService = new FakeTopologyService();
        var service = new RoutingService(topologyService, connectionClient, auditService, new ConnectionCompatibilityEvaluator(), new RoutingMatrixService());

        var result = await service.ConnectAsync(
            new RouteConnectCommand(
                "receiver-a",
                "sender-a",
                "operator",
                ActivationRequest.Immediate(DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(connectionClient.LastRequest);
        Assert.Equal(ConnectionOperation.Connect, connectionClient.LastRequest!.Operation);
        Assert.Equal("sender-a", connectionClient.LastRequest.SenderId);
    }

    [Fact]
    public async Task ValidateAsync_WhenFormatsMismatch_ReturnsIncompatible()
    {
        var connectionClient = new RecordingConnectionClient();
        var auditService = new RecordingAuditService();
        var topologyService = new FakeTopologyService(
            new NmosSenderDto(
                "sender-a",
                "node-a",
                "device-a",
                null,
                "Sender A",
                NmosTransportType.Rtp,
                new MediaFormatSummary("urn:x-nmos:format:video", "video/raw", null, null, null, null),
                null,
                null,
                new TransportFileData("application/sdp", "v=0"),
                "Video",
                "source-a",
                "Sender A",
                null,
                "A",
                true,
                DateTimeOffset.UtcNow));

        var service = new RoutingService(topologyService, connectionClient, auditService, new ConnectionCompatibilityEvaluator(), new RoutingMatrixService());

        var result = await service.ValidateAsync(
            new RouteValidationCommand(
                "receiver-a",
                "sender-a",
                ActivationRequest.Immediate(DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.Equal(CompatibilityStatus.Incompatible, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == "format.mismatch");
    }

    private sealed class FakeTopologyService(NmosSenderDto? senderOverride = null) : ITopologyService
    {
        public Task<TopologyGraphDto> GetTopologyAsync(bool forceRefresh, CancellationToken cancellationToken)
        {
            var sender = senderOverride ?? new NmosSenderDto(
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
                DateTimeOffset.UtcNow);

            var receiver = new NmosReceiverDto(
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
                DateTimeOffset.UtcNow);

            return Task.FromResult(
                new TopologyGraphDto(
                    new RegistrySummaryDto(Guid.Empty, "registry", "http://mock", "v1.3", "v1.1", ControllerMode.Mock, true),
                    Array.Empty<NmosNodeDto>(),
                    Array.Empty<NmosDeviceDto>(),
                    Array.Empty<NmosSourceDto>(),
                    Array.Empty<NmosFlowDto>(),
                    [sender],
                    [receiver],
                    [new RoutingDestinationSnapshotDto("receiver-a", "Receiver A", "node-a", "device-a", null, "receiver-a", null, Array.Empty<string>())],
                    Array.Empty<TopologyRouteEdgeDto>(),
                    DateTimeOffset.UtcNow));
        }

        public Task<IReadOnlyCollection<NmosSenderDto>> GetSendersAsync(bool forceRefresh, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<NmosSenderDto>>(Array.Empty<NmosSenderDto>());

        public Task<IReadOnlyCollection<NmosReceiverDto>> GetReceiversAsync(bool forceRefresh, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<NmosReceiverDto>>(Array.Empty<NmosReceiverDto>());

        public Task<ResourceDetailDto?> GetResourceAsync(string resourceId, CancellationToken cancellationToken) =>
            Task.FromResult<ResourceDetailDto?>(null);
    }

    private sealed class RecordingConnectionClient : INmosConnectionClient
    {
        public ConnectionRequest? LastRequest { get; private set; }

        public Task ApplyConnectionAsync(ConnectionRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        public Task<NmosReceiver?> GetReceiverStateAsync(string receiverId, CancellationToken cancellationToken) =>
            Task.FromResult<NmosReceiver?>(null);
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public List<CreateAuditEntryCommand> Entries { get; } = [];

        public Task<IReadOnlyCollection<AuditEntryDto>> GetRecentAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<AuditEntryDto>>(Array.Empty<AuditEntryDto>());

        public Task RecordAsync(CreateAuditEntryCommand command, CancellationToken cancellationToken)
        {
            Entries.Add(command);
            return Task.CompletedTask;
        }
    }
}
