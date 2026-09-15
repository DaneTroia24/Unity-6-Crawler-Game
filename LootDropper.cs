using System.Collections.Generic;
using UnityEngine;

/// <summary>One resolved drop: the item that was rolled and how many of it.</summary>
public struct LootResult
{
    public ItemData Item;
    public int      Quantity;

    public LootResult(ItemData item, int quantity)
    {
        Item     = item;
        Quantity = quantity;
    }
}

/// <summary>
/// Turns a LootTable into concrete drops. Stateless, so the drop rules can be called
/// from anywhere (enemy death, chest open, a test).
///
/// Each entry is rolled independently against its own DropChance; the survivors are
/// shuffled and the first maxDrops are kept. Two consequences worth knowing:
///   - A DropChance of 1.0 always passes its roll but can still be cut by maxDrops,
///     so truly guaranteed drops must be routed around this class (ChestLoot does).
///   - If fewer entries pass than maxDrops allows, nothing pads the result out.
/// </summary>
public static class LootDropper
{
    /// <summary>
    /// Rolls up to maxDrops items from a table.
    /// Returns an empty list (never null) if the table is null or empty.
    /// </summary>
    public static List<LootResult> RollLoot(LootTable table, int maxDrops)
    {
        var results = new List<LootResult>();
        if (table == null || table.Entries == null || table.Entries.Count == 0)
            return results;

        var candidates = new List<LootResult>();
        foreach (LootEntry entry in table.Entries)
        {
            if (entry.Item == null) continue;
            if (Random.value <= entry.DropChance)
                candidates.Add(new LootResult(entry.Item, entry.RollQuantity()));
        }

        // Without this, a table with more candidates than maxDrops would always
        // favour whichever entries the designer happened to list first.
        Shuffle(candidates);

        int take = Mathf.Min(maxDrops, candidates.Count);
        for (int i = 0; i < take; i++)
            results.Add(candidates[i]);

        return results;
    }

    // Fisher-Yates: every ordering is equally likely.
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j    = Random.Range(0, i + 1);
            T   temp = list[i];
            list[i]  = list[j];
            list[j]  = temp;
        }
    }
}
