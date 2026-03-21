using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NmosController.Application.Topology;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Json;

namespace NmosController.Infrastructure.Nmos.Mock;

public sealed class MockNmosFixtureStore
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<MockNmosFixtureStore> _logger;
    private TopologySnapshotDto _snapshot;

    public MockNmosFixtureStore(IOptions<NmosControllerOptions> options, ILogger<MockNmosFixtureStore> logger)
    {
        _logger = logger;
        _snapshot = LoadSnapshot(options.Value.MockLab.FixturePath);
    }

    public async Task<TopologySnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return Clone(_snapshot);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<NmosReceiver?> GetReceiverAsync(string receiverId, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var receiver = _snapshot.Receivers.FirstOrDefault(x => x.Id == receiverId);
            if (receiver is null)
            {
                return null;
            }

            return new NmosReceiver
            {
                Id = receiver.Id,
                NodeId = receiver.NodeId,
                DeviceId = receiver.DeviceId,
                Label = receiver.Label,
                Transport = receiver.Transport,
                Format = receiver.Format,
                Constraints = receiver.Constraints,
                Active = receiver.Active,
                Staged = receiver.Staged,
                IsConnectable = receiver.IsConnectable,
                LastSeenAtUtc = receiver.LastSeenAtUtc
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ApplyConnectionAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var senders = _snapshot.Senders.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
            var receivers = _snapshot.Receivers.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            if (!receivers.TryGetValue(request.ReceiverId, out var receiver))
            {
                throw new InvalidOperationException($"Mock receiver '{request.ReceiverId}' was not found.");
            }

            TransportFileData? transportFile = null;
            if (request.Operation == ConnectionOperation.Connect && request.SenderId is not null)
            {
                if (!senders.TryGetValue(request.SenderId, out var sender))
                {
                    throw new InvalidOperationException($"Mock sender '{request.SenderId}' was not found.");
                }

                transportFile = sender.TransportFile;
                senders[request.SenderId] = sender with { SubscribedReceiverId = request.ReceiverId };
            }

            foreach (var sender in senders.Values.Where(x => x.SubscribedReceiverId == request.ReceiverId))
            {
                senders[sender.Id] = sender with
                {
                    SubscribedReceiverId = request.Operation == ConnectionOperation.Connect && sender.Id == request.SenderId
                        ? request.ReceiverId
                        : null
                };
            }

            var newState = new ConnectionState(
                request.Operation == ConnectionOperation.Connect ? request.SenderId : null,
                request.Operation == ConnectionOperation.Connect ? "true" : "false",
                receiver.Active.TransportParameters,
                transportFile);

            var updatedReceiver = request.Activation.Mode == ActivationModeType.Immediate
                ? receiver with { Active = newState, Staged = newState, LastSeenAtUtc = DateTimeOffset.UtcNow }
                : receiver with { Staged = newState, LastSeenAtUtc = DateTimeOffset.UtcNow };

            receivers[request.ReceiverId] = updatedReceiver;

            _snapshot = _snapshot with
            {
                Senders = senders.Values.OrderBy(x => x.Label).ToArray(),
                Receivers = receivers.Values.OrderBy(x => x.Label).ToArray(),
                RetrievedAtUtc = DateTimeOffset.UtcNow
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private TopologySnapshotDto LoadSnapshot(string configuredPath)
    {
        var candidatePaths = new[]
        {
            configuredPath,
            Path.Combine(AppContext.BaseDirectory, configuredPath),
            Path.Combine(AppContext.BaseDirectory, "Nmos", "Mock", "Fixtures", "topology-snapshot.json")
        };

        var snapshotPath = candidatePaths.FirstOrDefault(File.Exists);
        if (snapshotPath is null)
        {
            throw new FileNotFoundException("Mock topology snapshot fixture was not found.", configuredPath);
        }

        var json = File.ReadAllText(snapshotPath);
        var snapshot = JsonSerializer.Deserialize<TopologySnapshotDto>(json, NmosJsonSerializer.Default);
        if (snapshot is null)
        {
            throw new InvalidOperationException($"Could not deserialize mock topology snapshot from '{snapshotPath}'.");
        }

        _logger.LogInformation("Loaded mock NMOS topology fixture from {FixturePath}", snapshotPath);
        return snapshot;
    }

    private static TopologySnapshotDto Clone(TopologySnapshotDto snapshot)
    {
        var json = JsonSerializer.Serialize(snapshot, NmosJsonSerializer.Default);
        return JsonSerializer.Deserialize<TopologySnapshotDto>(json, NmosJsonSerializer.Default)
               ?? throw new InvalidOperationException("Failed to clone mock topology snapshot.");
    }
}
