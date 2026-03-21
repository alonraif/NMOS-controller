using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NmosController.Domain.Entities;
using NmosController.Domain.Enums;
using NmosController.Domain.ValueObjects;
using NmosController.Infrastructure.Configuration;
using NmosController.Infrastructure.Nmos.Mock;

namespace NmosController.Infrastructure.Tests;

public sealed class MockNmosFixtureStoreTests
{
    [Fact]
    public async Task GetSnapshotAsync_LoadsFixtureData()
    {
        var store = CreateStore();

        var snapshot = await store.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(19, snapshot.Senders.Count);
        Assert.Equal(14, snapshot.Receivers.Count);
        Assert.Equal(8, snapshot.RoutingDestinations.Count);
        Assert.Contains(snapshot.Senders, x => x.Id == "sender-video-cam5-a" && x.SourceGroupLabel == "CAM-05" && x.PathType == "A");
        Assert.Contains(snapshot.Senders, x => x.Id == "sender-audio-evs-a-b" && x.SourceGroupLabel == "EVS-A" && x.PathType == "B");
        Assert.Contains(snapshot.Senders, x => x.Id == "sender-anc-pgma-alt-a" && x.SourceGroupLabel == "PGMA-ALT" && x.PathType == "A");
        Assert.Contains(snapshot.Receivers, x => x.Id == "receiver-dest-mcr-mon-08-video" && x.Active.SenderId is null);
        Assert.Contains(snapshot.Receivers, x => x.Id == "receiver-dest-mcr-mon-07-video" && x.Active.SenderId != x.Staged.SenderId);
        Assert.Contains(snapshot.RoutingDestinations, x => x.Id == "dest-mcr-mon-07" && x.Tags.Contains("Breakaway"));
    }

    [Fact]
    public async Task ApplyConnectionAsync_UpdatesReceiverAndSenderSubscription()
    {
        var store = CreateStore();

        await store.ApplyConnectionAsync(
            new ConnectionRequest
            {
                Operation = ConnectionOperation.Connect,
                ReceiverId = "receiver-dest-audio-room-audio",
                SenderId = "sender-audio-program-b",
                Activation = ActivationRequest.Immediate(DateTimeOffset.UtcNow),
                RequestedBy = "test"
            },
            CancellationToken.None);

        var snapshot = await store.GetSnapshotAsync(CancellationToken.None);
        var receiver = snapshot.Receivers.Single(x => x.Id == "receiver-dest-audio-room-audio");
        var sender = snapshot.Senders.Single(x => x.Id == "sender-audio-program-b");
        var sibling = snapshot.Senders.Single(x => x.Id == "sender-audio-program-a");

        Assert.Equal("sender-audio-program-b", receiver.Active.SenderId);
        Assert.Equal("receiver-dest-audio-room-audio", sender.SubscribedReceiverId);
        Assert.Equal("receiver-dest-audio-room-audio", sibling.SubscribedReceiverId);
    }

    private static MockNmosFixtureStore CreateStore()
    {
        var fixturePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "src",
                "backend",
                "NmosController.Infrastructure",
                "Nmos",
                "Mock",
                "Fixtures",
                "topology-snapshot.json"));

        var options = Options.Create(
            new NmosControllerOptions
            {
                MockLab = new MockLabOptions
                {
                    FixturePath = fixturePath
                }
            });

        return new MockNmosFixtureStore(options, NullLogger<MockNmosFixtureStore>.Instance);
    }
}
