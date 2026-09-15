using UnityEngine;

/// <summary>
/// Base class for every interactable world object (chests, doors, levers).
/// InteractionSystem only ever sees this type, so new interactables need no changes
/// there: inherit, override the three abstract members, and optionally drop a
/// Lockable component alongside to require a key.
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    /// <summary>Text shown in the interaction prompt HUD.</summary>
    public abstract string GetDisplayName();

    /// <summary>Highlight color applied while the player looks at this object.</summary>
    public abstract Color GetHighlightColor();

    /// <summary>Called when the player presses the interact key.</summary>
    public abstract void OnInteract();

    /// <summary>
    /// Whether the player may interact right now. Override to add conditions such as
    /// "already opened", calling base to keep the lock check.
    /// </summary>
    public virtual bool CanInteract()
    {
        // Handles locking for any subclass: TryUnlock consumes a matching key from
        // the player's inventory, so this call has side effects when it succeeds.
        Lockable lockable = GetComponent<Lockable>();
        if (lockable != null && lockable.IsLocked)
            return lockable.TryUnlock();

        return true;
    }

    /// <summary>Called once when the player starts looking at this object.</summary>
    public virtual void OnFocusEnter() { }

    /// <summary>Called once when the player stops looking at this object.</summary>
    public virtual void OnFocusExit() { }
}
