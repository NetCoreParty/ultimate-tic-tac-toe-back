using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using UltimateTicTacToe.Core.Domain.Events;

namespace UltimateTicTacToe.Storage.Extensions;

public static class EventStoreExtensions
{
    public static void AddGlobalMongoSerialization(this IServiceCollection services)
    {
        try
        {
            // Safe to attempt: if it's already registered, MongoDB driver will throw.
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch
        {
            // Ignore duplicate registration (global static registry).
        }

        RegisterClassMapIfMissing<DomainEventBase>(cm =>
        {
            cm.AutoMap();
            cm.SetIsRootClass(true);
        });

        // Register all known domain events for safe polymorphic (de)serialization.
        RegisterClassMapIfMissing<GameCreatedEvent>(cm => cm.AutoMap());
        RegisterClassMapIfMissing<CellMarkedEvent>(cm => cm.AutoMap());
        RegisterClassMapIfMissing<MiniBoardWonEvent>(cm => cm.AutoMap());
        RegisterClassMapIfMissing<MiniBoardDrawnEvent>(cm => cm.AutoMap());
        RegisterClassMapIfMissing<FullGameWonEvent>(cm => cm.AutoMap());
        RegisterClassMapIfMissing<GameDrawnEvent>(cm => cm.AutoMap());
    }

    private static void RegisterClassMapIfMissing<T>(Action<BsonClassMap<T>> map) where T : class
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
            return;

        BsonClassMap.RegisterClassMap(map);
    }
}