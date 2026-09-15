using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A lootable chest. Contents come either from a hand-picked list (useStaticLoot,
/// for story chests that must be predictable) or from a rolled LootTable.
///
/// The optional guaranteed drop bypasses LootDropper on purpose: an entry inside a
/// table can still be cut by the maxDrops limit, so it would not truly be guaranteed.
///
/// Add a Lockable component to require a key - the base class handles it.
/// </summary>
public class ChestLoot : Interactable
{
    [Header("Chest Identity")]
    [SerializeField] private string chestName = "Chest";

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(1.00f, 0.85f, 0.40f, 1f);

    [Header("Engram Spawn Point")]
    [Tooltip("Child Transform where engrams spawn. Falls back to this object's position.")]
    [SerializeField] private Transform spawnPoint;

    [Header("Gold Drop")]
    [Tooltip("Minimum gold awarded when the chest is opened. Set both to 0 for no gold.")]
    [Min(0)] [SerializeField] private int goldMin = 0;
    [Tooltip("Maximum gold awarded when the chest is opened.")]
    [Min(0)] [SerializeField] private int goldMax = 0;

    [Header("Loot Mode")]
    [Tooltip("True = static guaranteed drops. False = randomized via LootTable.")]
    [SerializeField] private bool useStaticLoot = false;

    [Header("Static Loot (UseStaticLoot = true)")]
    [SerializeField] private List<LootEntry> staticDrops = new List<LootEntry>();

    [Header("Randomized Loot (UseStaticLoot = false)")]
    [SerializeField] private LootTable lootTable;

    [Tooltip("Maximum number of items rolled from the loot table, on top of the guaranteed drop.")]
    [Range(0, 10)]
    [SerializeField] private int bonusDropCount = 2;

    [Header("Guaranteed Drop (both modes)")]
    [Tooltip("Always drops. Leave Item null to skip.")]
    [SerializeField] private LootEntry guaranteedDrop;

    private bool isOpened = false;

    public bool   IsOpened  => isOpened;
    public string ChestName => chestName;

    public override string GetDisplayName()
    {
        // Once opened the chest is inert, so drop any "[Locked]" prefix a Lockable
        // would otherwise still be adding.
        if (isOpened) return chestName;

        Lockable lockable = GetComponent<Lockable>();
        if (lockable != null) return lockable.FormatDisplayName(chestName);
        return chestName;
    }

    public override Color GetHighlightColor() => highlightColor;

    public override bool CanInteract()
    {
        if (isOpened) return false;
        return base.CanInteract();  // Runs the Lockable key check.
    }

    public override void OnInteract()
    {
        if (isOpened) return;

        // Set before spawning so a second interact in the same frame cannot
        // double-award the contents.
        isOpened = true;
        AwardGold();
        SpawnLoot();

        Debug.Log($"[ChestLoot] '{chestName}' opened!");

        InteractionSystem.Instance?.ClearCurrentInteractable();
    }

    private void AwardGold()
    {
        if (goldMax <= 0) return;
        int gold = Random.Range(goldMin, goldMax + 1);
        if (gold > 0)
            PlayerCharacter.Instance?.Gold.AddGold(gold);
    }

    private void SpawnLoot()
    {
        if (EngramSpawner.Instance == null)
        {
            Debug.LogWarning("[ChestLoot] EngramSpawner.Instance is null, no loot spawned.");
            return;
        }

        LootResult? guaranteed = null;
        if (guaranteedDrop != null && guaranteedDrop.Item != null)
            guaranteed = new LootResult(guaranteedDrop.Item, guaranteedDrop.RollQuantity());

        List<LootResult> rolled = useStaticLoot
            ? BuildStaticLoot()
            : LootDropper.RollLoot(lootTable, bonusDropCount);

        // Directed along the chest's forward axis so drops scatter out of the front
        // rather than through the lid.
        Vector3 origin  = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 forward = transform.forward;

        EngramSpawner.Instance.SpawnDropsDirected(origin, forward, guaranteed, rolled);
    }

    /// <summary>
    /// Converts the hand-picked entries into results directly, skipping LootDropper
    /// so every listed entry drops regardless of its DropChance. Quantities still roll.
    /// </summary>
    private List<LootResult> BuildStaticLoot()
    {
        var results = new List<LootResult>();
        foreach (LootEntry entry in staticDrops)
        {
            if (entry == null || entry.Item == null) continue;
            results.Add(new LootResult(entry.Item, entry.RollQuantity()));
        }
        return results;
    }

    // Draws the spawn origin and drop direction so the spawn point can be positioned
    // in the Scene view without entering play mode.
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, 0.15f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(origin, transform.forward * 1.5f);
    }
}
