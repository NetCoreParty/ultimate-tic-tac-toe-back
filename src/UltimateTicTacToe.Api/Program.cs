using MongoDB.Bson;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Storage.Services;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GamePlay;
using UltimateTicTacToe.Core.Features.RealTimeMoveUpdates;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            #region Game Repository

            builder.Services.Configure<GameplaySettings>(builder.Configuration.GetSection("GameplaySettings"));
            builder.Services.AddSingleton<IGameRepository, InMemoryGameRepository>();

            #endregion

            #region Event Store

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            // Register base class and its known types
            BsonClassMap.RegisterClassMap<DomainEventBase>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });

            BsonClassMap.RegisterClassMap<CellMarkedEvent>(cm => cm.AutoMap());

            builder.Services.Configure<EventStoreSettings>(builder.Configuration.GetSection("EventStoreSettings"));
            builder.Services.AddSingleton<IEventStore, MongoEventStore>();

            builder.Services.AddHostedService<EventStoreInitializer>();

            #endregion

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddControllers();
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MakeMoveCommand).Assembly));
            builder.Services.AddSignalR();

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
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(corsConfig.PolicyName);

            app.UseRouting();

            app.UseAuthorization();
            app.UseHttpsRedirection();

            app.MapHub<MoveUpdatesHub>("/move-updates-hub")
                .RequireCors(corsConfig.PolicyName);

            app.MapControllers();

            app.Run();
        }
    }
}