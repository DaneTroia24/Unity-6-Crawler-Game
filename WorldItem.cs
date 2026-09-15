using UnityEngine;

/// <summary>
/// A pickup lying on the ground. Holds one of two things: an ItemData plus a quantity
/// for stackables, or a WeaponInstance for weapons, which carry their own level and
/// XP and so must survive a drop-and-pick-up as the same instance.
///
/// InteractionSystem raycasts for this component directly instead of going through
/// Interactable, since pickups need no CanInteract() gate and no per-type behaviour.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private ItemData itemData;
    [SerializeField] private int      quantity = 1;

    // Set only when this pickup came from a dropped weapon; null otherwise, which is
    // what IsWeapon tests for.
    private WeaponInstance weaponInstance;

    public ItemData       ItemData       => itemData;
    public int            Quantity       => quantity;
    public WeaponInstance WeaponInstance => weaponInstance;
    public bool           IsWeapon       => itemData is WeaponData && weaponInstance != null;

    /// <summary>Initializes from a plain item such as a consumable or material.</summary>
    public void Initialize(ItemData data, int qty = 1)
    {
        itemData       = data;
        quantity       = qty;
        weaponInstance = null;
    }

    /// <summary>
    /// Initializes from an existing weapon, keeping its level and XP intact.
    /// Quantity is forced to 1 because weapon instances are unique and cannot stack.
    /// </summary>
    public void InitializeFromWeapon(WeaponInstance weapon)
    {
        itemData       = weapon.Data;
        quantity       = 1;
        weaponInstance = weapon;
    }

    /// <summary>Prompt label: weapons show their level, stacks show their count.</summary>
    public string GetDisplayName()
    {
        if (itemData == null) return "Unknown Item";

        if (IsWeapon)
            return $"{itemData.ItemName}  [Lv.{weaponInstance.Level}]";

        if (quantity > 1)
            return $"{itemData.ItemName}  x{quantity}";

        return itemData.ItemName;
    }

    /// <summary>Rarity drives the highlight color InteractionSystem applies.</summary>
    public ItemRarity GetRarity()
    {
        return itemData != null ? itemData.Rarity : ItemRarity.Common;
    }

    /// <summary>
    /// Applies a one-off impulse so a dropped item arcs away from the player instead
    /// of landing on their feet.
    /// </summary>
    public void Toss(Vector3 direction, float force = 2f)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(direction * force, ForceMode.Impulse);
    }
}
