using UnityEngine;

/// <summary>
/// A one-way door. Doors here are passages rather than containers, so once open the
/// GameObject deactivates and stops being a raycast target.
///
/// If a doorAnimator is assigned, its "unlock" animation must call
/// OnOpenAnimationComplete from an Animation Event on its final frame.
/// Add a Lockable component to require a key.
/// </summary>
public class DoorController : Interactable
{
    [Header("Door Identity")]
    [SerializeField] private string doorName = "Door";

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = new Color(0.80f, 0.60f, 0.30f, 1f);

    [Header("Animation")]
    [Tooltip("Animator on the door model. Must have a trigger parameter named 'unlock'.")]
    [SerializeField] private Animator doorAnimator;

    // Cached: the string overloads of SetTrigger re-hash the name on every call.
    private static readonly int OpenTriggerHash = Animator.StringToHash("unlock");

    private bool isOpen = false;

    public bool   IsOpen   => isOpen;
    public string DoorName => doorName;

    public override string GetDisplayName()
    {
        Lockable lockable = GetComponent<Lockable>();
        if (lockable != null)
            return lockable.FormatDisplayName(doorName);

        return doorName;
    }

    public override Color GetHighlightColor() => highlightColor;

    public override bool CanInteract()
    {
        if (isOpen) return false;
        return base.CanInteract();  // Runs the Lockable key check.
    }

    public override void OnInteract()
    {
        Open();
    }

    /// <summary>
    /// Opens the door. Public so scripted events (a lever, a cutscene) can open it
    /// without going through the interaction prompt.
    /// </summary>
    public void Open()
    {
        if (isOpen) return;

        isOpen = true;

        if (doorAnimator != null)
            doorAnimator.SetTrigger(OpenTriggerHash);
        else
            OnOpenAnimationComplete();  // No animator, so skip to the end state.

        InteractionSystem.Instance?.ClearCurrentInteractable();
    }

    /// <summary>
    /// Deactivates the door so the player can walk through. Called by an Animation
    /// Event on the last frame of the open animation, or by Open() when there is no
    /// Animator assigned.
    /// </summary>
    public void OnOpenAnimationComplete()
    {
        gameObject.SetActive(false);
    }
}
