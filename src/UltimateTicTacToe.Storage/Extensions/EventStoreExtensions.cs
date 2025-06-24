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
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        // Register base class and its known types
        BsonClassMap.RegisterClassMap<DomainEventBase>(cm =>
        {
            cm.AutoMap();
            cm.SetIsRootClass(true);
        });

        BsonClassMap.RegisterClassMap<CellMarkedEvent>(cm => cm.AutoMap());
    }
}