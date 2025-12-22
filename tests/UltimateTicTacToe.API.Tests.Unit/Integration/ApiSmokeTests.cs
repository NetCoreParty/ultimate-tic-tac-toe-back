using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using UltimateTicTacToe.Core;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.API.Tests.Unit.Integration;

public class ApiSmokeTests
{
    private sealed class ApiFactory : WebApplicationFactory<UltimateTicTacToe.API.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Prevent Mongo hosted services from running in tests.
                services.RemoveAll<IHostedService>();

                // Use in-memory event store for API smoke tests.
                services.RemoveAll<IEventStore>();
                services.AddSingleton<IEventStore, InMemoryEventStore>();
            });
        }
    }

    [Fact]
    public async Task StartMoveAndMovesHistory_ShouldWork_EndToEnd()
    {
        await using var factory = new ApiFactory();
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
}

