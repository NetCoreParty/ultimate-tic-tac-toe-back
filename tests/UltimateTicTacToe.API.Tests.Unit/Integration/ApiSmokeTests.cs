using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using UltimateTicTacToe.Core;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSaving;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Core.Features.Rooms;
using UltimateTicTacToe.API.Tests.Unit.Integration.TestDoubles;

namespace UltimateTicTacToe.API.Tests.Unit.Integration;

public class ApiSmokeTests
{
    private sealed class ApiFactory : WebApplicationFactory<UltimateTicTacToe.API.Program>
    {
        private readonly InMemoryEventStore _eventStore;
        private readonly IStateSnapshotStore _snapshots;
        private readonly InMemoryRoomStore _rooms;
        private readonly InMemoryMatchmakingTicketStore _tickets;
        private readonly InMemoryRoomMetricsStore _metrics;

        public ApiFactory(
            InMemoryEventStore eventStore,
            IStateSnapshotStore snapshots,
            InMemoryRoomStore rooms,
            InMemoryMatchmakingTicketStore tickets,
            InMemoryRoomMetricsStore metrics)
        {
            _eventStore = eventStore;
            _snapshots = snapshots;
            _rooms = rooms;
            _tickets = tickets;
            _metrics = metrics;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Prevent Mongo hosted services from running in tests.
                services.RemoveAll<IHostedService>();

                // Use in-memory event store for API smoke tests.
                services.RemoveAll<IEventStore>();
                services.AddSingleton<IEventStore>(_eventStore);

                // Use in-memory snapshots for tests (Mongo snapshots are covered in storage integration tests).
                services.RemoveAll<IStateSnapshotStore>();
                services.AddSingleton(_snapshots);

                // Use in-memory rooms/tickets so tests don't require Mongo.
                services.RemoveAll<IRoomStore>();
                services.RemoveAll<IMatchmakingTicketStore>();
                services.RemoveAll<IRoomMetricsStore>();
                services.AddSingleton<IRoomStore>(_rooms);
                services.AddSingleton<IMatchmakingTicketStore>(_tickets);
                services.AddSingleton<IRoomMetricsStore>(_metrics);
            });
        }
    }

    [Fact]
    public async Task StartMoveAndMovesHistory_ShouldWork_EndToEnd()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics);
        var client = factory.CreateClient();

        // Start
        var startResponse = await client.PostAsync("/api/game/start", content: null);
        startResponse.EnsureSuccessStatusCode();

        using var startJson = JsonDocument.Parse(await startResponse.Content.ReadAsStringAsync());
        Assert.True(startJson.RootElement.GetProperty("isSuccess").GetBoolean());
        var startValue = startJson.RootElement.GetProperty("value");

        var gameId = startValue.GetProperty("gameId").GetGuid();
        var playerX = startValue.GetProperty("playerXId").GetGuid();

        // Move
        var move = new PlayerMoveRequest(gameId, playerX, MiniBoardRowId: 0, MiniBoardColId: 0, CellRowId: 0, CellColId: 0);
        var moveResponse = await client.PostAsJsonAsync("/api/game/move", move);
        moveResponse.EnsureSuccessStatusCode();

        // Moves history
        var historyResponse = await client.GetAsync($"/api/game-management/{gameId}/moves-history?skip=0&take=10");
        historyResponse.EnsureSuccessStatusCode();

        using var historyJson = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        Assert.True(historyJson.RootElement.GetProperty("isSuccess").GetBoolean());
        var moves = historyJson.RootElement.GetProperty("value").GetProperty("moves");
        Assert.Equal(1, moves.GetArrayLength());
    }

    [Fact]
    public async Task RestartDurability_MovesHistory_ShouldSurvive_NewHostInstance()
    {
        var store = new InMemoryEventStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        Guid gameId;
        Guid playerX;

        // First host
        {
            var snapshots1 = new StateSnapshotStore();
            await using var factory1 = new ApiFactory(store, snapshots1, rooms, tickets, metrics);
            var client1 = factory1.CreateClient();

            var startResponse = await client1.PostAsync("/api/game/start", content: null);
            startResponse.EnsureSuccessStatusCode();
            using var startJson = JsonDocument.Parse(await startResponse.Content.ReadAsStringAsync());
            var startValue = startJson.RootElement.GetProperty("value");
            gameId = startValue.GetProperty("gameId").GetGuid();
            playerX = startValue.GetProperty("playerXId").GetGuid();

            var move = new PlayerMoveRequest(gameId, playerX, 0, 0, 0, 0);
            var moveResponse = await client1.PostAsJsonAsync("/api/game/move", move);
            moveResponse.EnsureSuccessStatusCode();
        }

        // Second host (simulated restart)
        {
            var snapshots2 = new StateSnapshotStore();
            await using var factory2 = new ApiFactory(store, snapshots2, rooms, tickets, metrics);
            var client2 = factory2.CreateClient();

            var historyResponse = await client2.GetAsync($"/api/game-management/{gameId}/moves-history?skip=0&take=10");
            historyResponse.EnsureSuccessStatusCode();

            using var historyJson = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
            var moves = historyJson.RootElement.GetProperty("value").GetProperty("moves");
            Assert.Equal(1, moves.GetArrayLength());
        }
    }

    [Fact]
    public async Task Rooms_RegularQueue_TwoUsers_ShouldCreateGame_AndAllowMove()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics);
        var client = factory.CreateClient();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var reqA = new HttpRequestMessage(HttpMethod.Post, "/api/rooms/queue");
        reqA.Headers.Add("X-User-Id", userA.ToString());
        var resA = await client.SendAsync(reqA);
        resA.EnsureSuccessStatusCode();
        using var jsonA = JsonDocument.Parse(await resA.Content.ReadAsStringAsync());
        var ticketA = jsonA.RootElement.GetProperty("value").GetProperty("ticketId").GetGuid();
        Assert.NotEqual(Guid.Empty, ticketA);

        var reqB = new HttpRequestMessage(HttpMethod.Post, "/api/rooms/queue");
        reqB.Headers.Add("X-User-Id", userB.ToString());
        var resB = await client.SendAsync(reqB);
        resB.EnsureSuccessStatusCode();
        using var jsonB = JsonDocument.Parse(await resB.Content.ReadAsStringAsync());
        var ticketB = jsonB.RootElement.GetProperty("value").GetProperty("ticketId").GetGuid();

        var t = tickets.TryGet(ticketB);
        Assert.NotNull(t);
        Assert.NotNull(t!.GameId);

        // First player (userA) is X; ensure move succeeds
        var move = new PlayerMoveRequest(t.GameId!.Value, userA, 0, 0, 0, 0);
        var moveRes = await client.PostAsJsonAsync("/api/game/move", move);
        moveRes.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Rooms_Private_CreateJoin_ShouldCreateGame_AndAllowMove()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics);
        var client = factory.CreateClient();

        var owner = Guid.NewGuid();
        var joiner = Guid.NewGuid();

        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/rooms/private");
        createReq.Headers.Add("X-User-Id", owner.ToString());
        var createRes = await client.SendAsync(createReq);
        createRes.EnsureSuccessStatusCode();

        using var createJson = JsonDocument.Parse(await createRes.Content.ReadAsStringAsync());
        var joinCode = createJson.RootElement.GetProperty("value").GetProperty("joinCode").GetString();
        Assert.False(string.IsNullOrWhiteSpace(joinCode));

        var joinReq = new HttpRequestMessage(HttpMethod.Post, $"/api/rooms/private/join/{joinCode}");
        joinReq.Headers.Add("X-User-Id", joiner.ToString());
        var joinRes = await client.SendAsync(joinReq);
        joinRes.EnsureSuccessStatusCode();

        using var joinJson = JsonDocument.Parse(await joinRes.Content.ReadAsStringAsync());
        var gameId = joinJson.RootElement.GetProperty("value").GetProperty("gameId").GetGuid();

        // Owner is X (players[0]) in our in-memory room store
        var move = new PlayerMoveRequest(gameId, owner, 0, 0, 0, 0);
        var moveRes = await client.PostAsJsonAsync("/api/game/move", move);
        moveRes.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Backpressure_ShouldReject_RoomsQueueAndPrivateCreate()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics)
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["GameplaySettings:MaxActiveGames"] = "2",
                        ["GameplaySettings:BackpressureThresholdPercent"] = "50" // threshold=1
                    });
                });
            });
        var client = factory.CreateClient();

        // Start 1 game to hit threshold (GamesNow=1, threshold=1)
        var startResponse = await client.PostAsync("/api/game/start", content: null);
        startResponse.EnsureSuccessStatusCode();

        var user = Guid.NewGuid();

        var queueReq = new HttpRequestMessage(HttpMethod.Post, "/api/rooms/queue");
        queueReq.Headers.Add("X-User-Id", user.ToString());
        var queueRes = await client.SendAsync(queueReq);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, queueRes.StatusCode);

        var privateReq = new HttpRequestMessage(HttpMethod.Post, "/api/rooms/private");
        privateReq.Headers.Add("X-User-Id", user.ToString());
        var privateRes = await client.SendAsync(privateReq);
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, privateRes.StatusCode);
    }
}

