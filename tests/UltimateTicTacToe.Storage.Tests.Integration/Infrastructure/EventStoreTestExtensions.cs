using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

public static class EventStoreTestExtensions
{
    public static bool IndexExists(
    List<MongoIndexInfo> indexInfos,
    string name,
    Dictionary<string, int> expectedKeyMap,
    bool? unique = null)
    {
        return indexInfos.Any(i =>
            i.Name == name &&
            i.KeyMap.SequenceEqual(expectedKeyMap) &&
            (unique == null || i.IsUnique == unique));
    }
}