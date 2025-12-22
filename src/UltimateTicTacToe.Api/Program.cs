using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Storage.Services;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Core.Features.GamePlay;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using UltimateTicTacToe.Storage.Extensions;
using UltimateTicTacToe.API.Hubs;
using UltimateTicTacToe.Core.Features.RealTimeNotification;
using UltimateTicTacToe.API.RealTimeNotification;
using Scalar.AspNetCore;

namespace UltimateTicTacToe.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        #region Games Repository

        builder.Services.Configure<GameplaySettings>(builder.Configuration.GetSection("GameplaySettings"));
        builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();

        #endregion

        #region Event Store

        builder.Services.Configure<EventStoreSettings>(builder.Configuration.GetSection("EventStoreSettings"));
        builder.Services.AddGlobalMongoSerialization();
        builder.Services.AddSingleton<IMongoDatabase>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<EventStoreSettings>>().Value;
            var client = new MongoClient(settings.ConnectionString);
            return client.GetDatabase(settings.DatabaseName);
        });
        builder.Services.AddTransient<IEventStore, MongoEventStore>();
        builder.Services.AddHostedService<EventStoreInitializer>();

        #endregion

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();

        builder.Services.AddControllers();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MakeMoveCommand).Assembly));

        #region Real Time Notifier

        builder.Services.AddSignalR();
        builder.Services.AddTransient<IMoveUpdatesNotificationHub, MoveUpdatesNotificationHub>();

        #endregion

        #region CORS

        builder.Services.Configure<CorsSettings>(builder.Configuration.GetSection("CorsSettings"));
        var corsConfig = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>() ?? new CorsSettings();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(corsConfig.PolicyName, policy =>
                policy
                    .WithOrigins(corsConfig.AllowedOrigins.ToArray())
                    .WithMethods("GET", "POST", "DELETE")
                    .WithHeaders("Content-Type", "Authorization", "X-User-Id", "X-Ultimate-TTT-Header")
                    .AllowCredentials()); // Only if you're using cookies or auth
        });

        #endregion

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.UseDeveloperExceptionPage();
        }

        app.UseCors(corsConfig.PolicyName);
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.MapHub<MoveUpdatesHub>("/move-updates-hub")
            .RequireCors(corsConfig.PolicyName);

        app.MapControllers();

        app.Run();
    }
}