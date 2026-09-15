using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A named pool of possible drops, authored as an asset so designers can tune loot
/// without touching code - one per enemy type or chest tier. Holds no logic;
/// LootDropper reads it at runtime to roll results.
/// </summary>
[CreateAssetMenu(fileName = "NewLootTable", menuName = "Loot/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("Pool")]
    [Tooltip("All possible items that can drop from this table. " +
             "Each entry has its own independent drop chance.")]
    public List<LootEntry> Entries = new List<LootEntry>();
}
