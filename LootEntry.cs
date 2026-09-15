using System;
using UnityEngine;

/// <summary>
/// A single row in a LootTable. Entries roll independently, so the chances in a table
/// do not need to add up to 1. See LootDropper for the full rules.
///
/// Serializable rather than a ScriptableObject so entries can be authored inline,
/// either inside a LootTable asset or directly on a chest.
/// </summary>
[Serializable]
public class LootEntry
{
    [Tooltip("The item that can drop.")]
    public ItemData Item;

    [Range(0f, 1f)]
    [Tooltip("Probability this entry drops (0 = never, 1 = always).")]
    public float DropChance = 0.5f;

    [Min(1)]
    [Tooltip("Lowest quantity to drop. Ignored for weapons, which always drop as a single instance.")]
    public int QuantityMin = 1;

    [Min(1)]
    [Tooltip("Highest quantity to drop, inclusive.")]
    public int QuantityMax = 1;

    /// <summary>Returns a random quantity between min and max, both inclusive.</summary>
    public int RollQuantity() => UnityEngine.Random.Range(QuantityMin, QuantityMax + 1);
}
