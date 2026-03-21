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

        Assert.NotEmpty(snapshot.Senders);
        Assert.NotEmpty(snapshot.Receivers);
    }

    [Fact]
    public async Task ApplyConnectionAsync_UpdatesReceiverAndSenderSubscription()
    {
        var store = CreateStore();

        await store.ApplyConnectionAsync(
            new ConnectionRequest
            {
                Operation = ConnectionOperation.Connect,
                ReceiverId = "receiver-audio-b",
                SenderId = "sender-audio-a",
                Activation = ActivationRequest.Immediate(DateTimeOffset.UtcNow),
                RequestedBy = "test"
            },
            CancellationToken.None);

        var snapshot = await store.GetSnapshotAsync(CancellationToken.None);
        var receiver = snapshot.Receivers.Single(x => x.Id == "receiver-audio-b");
        var sender = snapshot.Senders.Single(x => x.Id == "sender-audio-a");

        Assert.Equal("sender-audio-a", receiver.Active.SenderId);
        Assert.Equal("receiver-audio-b", sender.SubscribedReceiverId);
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
