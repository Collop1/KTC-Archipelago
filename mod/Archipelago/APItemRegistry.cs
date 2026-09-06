using System.Collections.Generic;
using System.Linq;

namespace APKingdom2Crown;

public static class APItemRegistry
{
    // Stores all item IDs and their collected status
    private static readonly Dictionary<int, bool> _items = new();

    // Add an item ID to the registry
    public static void RegisterItem(int itemId)
    {
        if (!_items.ContainsKey(itemId))
            _items[itemId] = Plugin.AP?.Session?.Items.AllItemsReceived?.Select(s => s.ItemId == itemId) != null;
    }

    // Mark an item as collected
    public static void CollectItem(int itemId)
    {
        if (_items.ContainsKey(itemId))
            _items[itemId] = true;
    }

    // Check if an item has been collected
    public static bool IsCollected(int itemId)
    {
        return _items.TryGetValue(itemId, out var collected) && collected;
    }

    // Optional: get all collected items
    public static IEnumerable<int> GetCollectedItems()
    {
        foreach (var kvp in _items)
            if (kvp.Value) yield return kvp.Key;
    }

    // Optional: initialize a batch of items
    public static void RegisterItems(IEnumerable<int> itemIds)
    {
        foreach (var id in itemIds)
            RegisterItem(id);
    }
}
