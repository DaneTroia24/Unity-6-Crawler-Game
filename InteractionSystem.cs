using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives the crosshair: one ray per frame resolves what the player is looking at,
/// updates the prompt and highlight, and the interact key acts on whatever it found.
///
/// Targets are resolved in priority order - EngramItem, then WorldItem, then
/// Interactable. Pickups win because they are small and often sit in front of the
/// object that spawned them. The two pickup kinds are handled concretely here since
/// picking up needs no per-type behaviour; anything with real behaviour goes through
/// Interactable, so this class never needs to know chests or doors exist.
///
/// Attach to: Player GameObject.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float     interactRange  = 3f;
    [Tooltip("Layers the interact ray can hit. Leave as Nothing to hit everything.")]
    [SerializeField] private LayerMask interactLayers;
    [Tooltip("Assign the Main Camera transform. Leave null to auto-find Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Header("World Item Spawning")]
    [Tooltip("Safety net prefab if an ItemData has no WorldPrefab assigned.")]
    [SerializeField] private GameObject fallbackWorldItemPrefab;
    [Tooltip("How far in front of the camera dropped items appear.")]
    [SerializeField] private float      dropSpawnDistance = 1.2f;
    [Tooltip("Impulse applied to dropped items so they arc away from the player.")]
    [SerializeField] private float      dropTossForce     = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    public static InteractionSystem Instance { get; private set; }

    // At most one of these three is non-null.
    private Interactable currentInteractable = null;
    private EngramItem   currentEngram       = null;
    private WorldItem    currentWorldItem    = null;

    // A picked-up object is destroyed during the input callback, but physics still
    // reports its collider for the rest of the frame. Skipping one UpdateLookTarget
    // stops the prompt flickering back on for an item that no longer exists.
    private bool skipLookTargetThisFrame = false;

    // Highlight colors by rarity. Interactables supply their own color instead.
    private static readonly Color ColorCommon    = new Color(1.00f, 1.00f, 1.00f, 1f);
    private static readonly Color ColorRare      = new Color(0.25f, 0.55f, 1.00f, 1f);
    private static readonly Color ColorLegendary = new Color(0.65f, 0.25f, 1.00f, 1f);

    private PlayerInputActions inputActions;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.Player.Disable();
    }

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (skipLookTargetThisFrame)
        {
            skipLookTargetThisFrame = false;
            return;
        }

        UpdateLookTarget();
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if      (currentEngram       != null) HandleEngramPickup();
        else if (currentWorldItem    != null) HandleWorldItemPickup();
        else if (currentInteractable != null) TryInteract(currentInteractable);
    }

    // On failure (a full inventory, say) nothing is cleared, so the prompt stays up
    // and the player can make room and retry. Same for HandleWorldItemPickup below.
    private void HandleEngramPickup()
    {
        bool success = PickupHandler.Instance.TryPickUpEngram(currentEngram);
        if (!success) return;

        HideCurrentHighlight();
        InteractionPromptUI.Instance?.Hide();
        currentEngram           = null;
        skipLookTargetThisFrame = true;
    }

    private void HandleWorldItemPickup()
    {
        bool success = PickupHandler.Instance.TryPickUpWorldItem(currentWorldItem);
        if (!success) return;

        HideCurrentHighlight();
        InteractionPromptUI.Instance?.Hide();
        currentWorldItem        = null;
        skipLookTargetThisFrame = true;
    }

    /// <summary>
    /// Casts the look ray and updates prompt and highlight when the target changes.
    /// Everything past the change check runs only on a transition, so the focus
    /// enter/exit callbacks stay balanced and the UI is not rebuilt every frame.
    /// </summary>
    private void UpdateLookTarget()
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.yellow);

        EngramItem   newEngram       = null;
        WorldItem    newWorldItem    = null;
        Interactable newInteractable = null;

        bool didHit = interactLayers != 0
            ? Physics.Raycast(ray, out RaycastHit hitInfo, interactRange, interactLayers)
            : Physics.Raycast(ray, out hitInfo, interactRange);

        if (didHit)
        {
            // GetComponentInParent, not GetComponent: the collider is often on a
            // child of the object that owns the component, such as a mesh under a
            // chest root.
            newEngram = hitInfo.collider.GetComponentInParent<EngramItem>();
            if (newEngram == null)
            {
                newWorldItem = hitInfo.collider.GetComponentInParent<WorldItem>();
                if (newWorldItem == null)
                    newInteractable = hitInfo.collider.GetComponentInParent<Interactable>();
            }
        }

        bool changed = newEngram       != currentEngram
                    || newWorldItem    != currentWorldItem
                    || newInteractable != currentInteractable;

        if (!changed) return;

        currentInteractable?.OnFocusExit();
        HideCurrentHighlight();

        currentEngram       = newEngram;
        currentWorldItem    = newWorldItem;
        currentInteractable = newInteractable;

        currentInteractable?.OnFocusEnter();

        if (currentEngram != null)
        {
            InteractionPromptUI.Instance?.ShowEngram(currentEngram);
            ShowHighlight(currentEngram.gameObject, RarityToColor(currentEngram.GetRarity()));
        }
        else if (currentWorldItem != null)
        {
            InteractionPromptUI.Instance?.ShowWorldItem(currentWorldItem);
            ShowHighlight(currentWorldItem.gameObject, RarityToColor(currentWorldItem.GetRarity()));
        }
        else if (currentInteractable != null)
        {
            InteractionPromptUI.Instance?.ShowInteractable(currentInteractable);
            ShowHighlight(currentInteractable.gameObject, currentInteractable.GetHighlightColor());
        }
        else
        {
            InteractionPromptUI.Instance?.Hide();
        }
    }

    private void TryInteract(Interactable interactable)
    {
        if (!interactable.CanInteract())
        {
            Debug.Log($"[InteractionSystem] Cannot interact with '{interactable.GetDisplayName()}'.");
            return;
        }

        interactable.OnInteract();

        // CanInteract may have consumed a key and unlocked the object, so refreshing
        // turns "[Locked] Door" into "Door" on that same key press.
        RefreshPrompt();
    }

    /// <summary>
    /// Drops the current interactable without waiting for the player to look away,
    /// for objects that stop being usable the moment they are used.
    /// </summary>
    public void ClearCurrentInteractable()
    {
        currentInteractable?.GetComponentInChildren<InteractableHighlight>()?.Hide();
        currentInteractable = null;
        InteractionPromptUI.Instance?.Hide();
    }

    /// <summary>Redraws the prompt for the current target without re-running the raycast.</summary>
    public void RefreshPrompt()
    {
        if      (currentEngram       != null) InteractionPromptUI.Instance?.ShowEngram(currentEngram);
        else if (currentWorldItem    != null) InteractionPromptUI.Instance?.ShowWorldItem(currentWorldItem);
        else if (currentInteractable != null) InteractionPromptUI.Instance?.ShowInteractable(currentInteractable);
    }

    // The highlight component is optional, so these no-op through ?. when an object
    // has no InteractableHighlight in its hierarchy.
    private static void ShowHighlight(GameObject target, Color color)
        => target.GetComponentInChildren<InteractableHighlight>()?.Show(color);

    private void HideCurrentHighlight()
    {
        if (currentEngram       != null) currentEngram.GetComponentInChildren<InteractableHighlight>()?.Hide();
        if (currentWorldItem    != null) currentWorldItem.GetComponentInChildren<InteractableHighlight>()?.Hide();
        if (currentInteractable != null) currentInteractable.GetComponentInChildren<InteractableHighlight>()?.Hide();
    }

    private static Color RarityToColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Rare      => ColorRare,
        ItemRarity.Legendary => ColorLegendary,
        _                    => ColorCommon,
    };

    /// <summary>
    /// Spawns the physical pickup for an item dropped from the inventory.
    /// Returns the spawned GameObject, or null if no usable prefab was available.
    /// </summary>
    public GameObject SpawnWorldItem(InventoryItem item)
    {
        if (item == null || item.IsEmpty) return null;

        GameObject prefabToSpawn = item.ItemData?.WorldPrefab ?? fallbackWorldItemPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[InteractionSystem] No WorldPrefab on '{item.ItemData?.ItemName}' and no fallback assigned.");
            return null;
        }

        GameObject go        = Instantiate(prefabToSpawn, GetDropPosition(), Quaternion.identity);
        WorldItem  worldItem = go.GetComponent<WorldItem>();

        if (worldItem == null)
        {
            Debug.LogError($"[InteractionSystem] Prefab '{prefabToSpawn.name}' missing WorldItem!");
            Destroy(go);
            return null;
        }

        // Weapons hand over the instance itself rather than a copy of the template,
        // so a levelled sword is still the same sword when picked back up.
        if (item.IsWeapon)
            worldItem.InitializeFromWeapon(item.WeaponInstance);
        else
            worldItem.Initialize(item.ItemData, item.Quantity);

        // Biased upward so the item arcs out instead of sliding along the floor.
        Vector3 tossDir = cameraTransform != null
            ? cameraTransform.forward + Vector3.up * 0.3f
            : Vector3.forward;

        worldItem.Toss(tossDir.normalized, dropTossForce);
        return go;
    }

    // In front of the camera and slightly below eye level, so the drop lands within
    // reach and stays visible as it falls.
    private Vector3 GetDropPosition()
    {
        if (cameraTransform == null) return transform.position + Vector3.up;
        return cameraTransform.position
               + cameraTransform.forward * dropSpawnDistance
               + Vector3.down * 0.3f;
    }
}
