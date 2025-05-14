using MongoDB.Bson;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Storage.Services;
using MongoDB.Bson.Serialization;
using UltimateTicTacToe.Core.Features.Game.Domain.Events;
using MongoDB.Bson.Serialization.Serializers;
using UltimateTicTacToe.Storage.HostedServices;
using Microsoft.Extensions.Configuration;
using UltimateTicTacToe.Core.Features.Gameplay;

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
            builder.Services.AddSignalR();

            #region CORS

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost8080_Only",
                    builder => builder
                        .WithOrigins("http://localhost:8080")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
            });

            #endregion

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }
            
            app.UseRouting();

            //app.UseCors("AllowLocalhost8080_Only");

            app.UseAuthorization();
            app.UseHttpsRedirection();

            //app.MapHub<GameHub>("game-hub");
                //.RequireCors("AllowLocalhost8080_Only");

            app.MapControllers();

            app.Run();
        }
    }
}
