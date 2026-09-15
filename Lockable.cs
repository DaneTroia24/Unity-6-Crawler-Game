using UnityEngine;

/// <summary>
/// Makes any Interactable require a key. Composable rather than a subclass: a locked
/// chest is just a ChestLoot with a Lockable next to it, and neither ChestLoot nor
/// DoorController knows about keys - Interactable.CanInteract() finds this component
/// and calls TryUnlock() itself.
///
/// Locks match keys by string ID: LockID must equal the KeyID on a KeyData asset in
/// the player's inventory.
/// </summary>
public class Lockable : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("Must match a KeyData.KeyID for the key that unlocks this object.")]
    [SerializeField] private string lockID = "";

    [Tooltip("Display prefix shown in the interaction prompt while locked. " +
             "Examples: '[Locked]', '[Sealed]', '[Barred]', '[Requires Key]'.")]
    [SerializeField] private string lockedPrefix = "[Locked]";

    [Header("Audio")]
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] private AudioClip unlockSound;

    private bool isLocked = true;

    public bool   IsLocked     => isLocked;
    public string LockID       => lockID;
    public string LockedPrefix => lockedPrefix;

    /// <summary>
    /// Attempts to unlock using a key from the player's inventory, returning false if
    /// they lack it. Unlocking is permanent for this object, so later interactions
    /// short-circuit and never consume a second key.
    /// </summary>
    public bool TryUnlock()
    {
        if (!isLocked) return true;

        KeySearchResult? result = KeyInventoryHelper.FindKey(lockID);

        if (!result.HasValue)
        {
            AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            Debug.Log($"[Lockable] '{gameObject.name}' is locked. " +
                      $"Player does not have key (LockID: {lockID}).");
            return false;
        }

        KeyData key = result.Value.Key;

        // Reusable keys stay in the inventory so they can open other objects sharing
        // the same LockID.
        if (key.IsOneTimeUse)
            KeyInventoryHelper.ConsumeKey(lockID);

        isLocked = false;
        AudioSource.PlayClipAtPoint(unlockSound, transform.position);
        Debug.Log($"[Lockable] '{gameObject.name}' unlocked with '{key.ItemName}'.");
        return true;
    }

    /// <summary>Re-locks the object, for puzzles that reset.</summary>
    public void Lock()
    {
        isLocked = true;
    }

    /// <summary>Unlocks without requiring or consuming a key, e.g. from a lever.</summary>
    public void ForceUnlock()
    {
        isLocked = false;
        Debug.Log($"[Lockable] '{gameObject.name}' force-unlocked.");
    }

    /// <summary>Wraps a name as "[Locked] Oak Door" while locked. Called from GetDisplayName().</summary>
    public string FormatDisplayName(string baseName)
    {
        return isLocked ? $"{lockedPrefix} {baseName}" : baseName;
    }
}
