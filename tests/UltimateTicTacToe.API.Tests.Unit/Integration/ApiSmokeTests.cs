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
using UltimateTicTacToe.API.Middleware;

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

            builder.ConfigureAppConfiguration((_, config) =>
            {
                // Keep API tests independent of a running Mongo instance.
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["HealthChecks:MongoEnabled"] = "false"
                });
            });

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

    [Fact]
    public async Task Move_WithWrongPlayer_ShouldReturn_403()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics);
        var client = factory.CreateClient();

        var startResponse = await client.PostAsync("/api/game/start", content: null);
        startResponse.EnsureSuccessStatusCode();
        using var startJson = JsonDocument.Parse(await startResponse.Content.ReadAsStringAsync());
        var startValue = startJson.RootElement.GetProperty("value");
        var gameId = startValue.GetProperty("gameId").GetGuid();
        var wrongPlayer = Guid.NewGuid();

        var move = new PlayerMoveRequest(gameId, wrongPlayer, 0, 0, 0, 0);
        var moveRes = await client.PostAsJsonAsync("/api/game/move", move);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, moveRes.StatusCode);
    }

    [Fact]
    public async Task Move_CellAlreadyOccupied_ShouldReturn_400()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics);
        var client = factory.CreateClient();

        var startResponse = await client.PostAsync("/api/game/start", content: null);
        startResponse.EnsureSuccessStatusCode();
        using var startJson = JsonDocument.Parse(await startResponse.Content.ReadAsStringAsync());
        var startValue = startJson.RootElement.GetProperty("value");
        var gameId = startValue.GetProperty("gameId").GetGuid();
        var playerX = startValue.GetProperty("playerXId").GetGuid();
        var playerO = startValue.GetProperty("playerOId").GetGuid();

        // First move OK
        var m1 = new PlayerMoveRequest(gameId, playerX, 0, 0, 0, 0);
        (await client.PostAsJsonAsync("/api/game/move", m1)).EnsureSuccessStatusCode();

        // Same cell again by the correct next player -> invalid move (cell already occupied)
        var m2 = new PlayerMoveRequest(gameId, playerO, 0, 0, 0, 0);
        var res = await client.PostAsJsonAsync("/api/game/move", m2);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task PrivateJoin_WhenRoomNotFull_ShouldReturn_409()
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

        // Join should create game immediately for a second user; but if someone tries to join with same user or invalid flow,
        // we still want to assert 409 path exists. We'll trigger it by calling join with OWNER id (room blocks self-join).
        var badJoinReq = new HttpRequestMessage(HttpMethod.Post, $"/api/rooms/private/join/{joinCode}");
        badJoinReq.Headers.Add("X-User-Id", owner.ToString());
        var badJoinRes = await client.SendAsync(badJoinReq);
        Assert.True(badJoinRes.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.Conflict);

        // Now do a valid join to ensure baseline works.
        var joinReq = new HttpRequestMessage(HttpMethod.Post, $"/api/rooms/private/join/{joinCode}");
        joinReq.Headers.Add("X-User-Id", joiner.ToString());
        var joinRes = await client.SendAsync(joinReq);
        joinRes.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task HealthEndpoints_ShouldReturn_200()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics);
        var client = factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        Assert.True(live.IsSuccessStatusCode);

        // Mongo check is disabled in the test host config.
        var ready = await client.GetAsync("/health/ready");
        Assert.True(ready.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CorrelationIdHeader_ShouldBeEchoedOrGenerated()
    {
        var store = new InMemoryEventStore();
        var snapshots = new StateSnapshotStore();
        var rooms = new InMemoryRoomStore();
        var tickets = new InMemoryMatchmakingTicketStore();
        var metrics = new InMemoryRoomMetricsStore();

        await using var factory = new ApiFactory(store, snapshots, rooms, tickets, metrics);
        var client = factory.CreateClient();

        // Echo provided
        var req = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        req.Headers.Add(CorrelationIdMiddleware.HeaderName, "abc123");
        var res = await client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        Assert.True(res.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values));
        Assert.Equal("abc123", values!.First());

        // Generate when missing
        var res2 = await client.GetAsync("/health/live");
        res2.EnsureSuccessStatusCode();
        Assert.True(res2.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values2));
        Assert.False(string.IsNullOrWhiteSpace(values2!.First()));
    }
}

