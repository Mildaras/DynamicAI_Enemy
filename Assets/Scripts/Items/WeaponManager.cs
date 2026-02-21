using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Where instantiated weapons will be parented.")]
    [SerializeField] private Transform weaponHolder;
    [Tooltip("Your Player's IK handler (to drive hand onto the weapon).")]
    [SerializeField] private WeaponIKHandler ikHandler;

    private GameObject currentWeaponModel;
    private IInventoryItem currentItem;

    /// <summary>
    /// Equip the given inventory item (or null to unequip).
    /// </summary>
    public void EquipWeapon(IInventoryItem item)
    {
        // 1) Destroy previous weapon
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentItem = item;

        // 2) If null, just clear IK and return
        if (item == null)
        {
            if (ikHandler != null)
                ikHandler.ikActive = false;
            return;
        }

        // 3) Instantiate the new weapon prefab under the holder
        currentWeaponModel = Instantiate(item.Prefab, weaponHolder);
        currentWeaponModel.transform.localPosition = Vector3.zero;
        currentWeaponModel.transform.localRotation = Quaternion.identity;

        // 4) Setup IK: look for a child named "RightHandAnchor"
        if (ikHandler != null)
        {
            Transform anchor = currentWeaponModel.transform.Find("RightHandAnchor");
            Debug.Log($"[WeaponManager] Anchor for {item.Name}: {(anchor==null?"<missing>":anchor.name)}");
            if (anchor != null)
            {
                ikHandler.rightHandTarget = anchor;
                ikHandler.ikActive        = true;
            }
            else
            {
                ikHandler.ikActive = false;
            }
        }

        // 5) Check if it's a staff (ranged) or a melee weapon
        if (currentWeaponModel.GetComponent<StaffController>() != null)
        {
            // StaffController drives its own behavior—no further setup needed here
        }
        else
        {
            // Must be melee: ensure a WeaponSwingController is attached
            var swing = currentWeaponModel.GetComponent<WeaponSwingController>()
                        ?? currentWeaponModel.AddComponent<WeaponSwingController>();

            // If the item provides IWeaponStats, inject those numbers
            if (item is IWeaponStats stats)
            {
                swing.damage    = stats.Damage;
                swing.minSpeed  = stats.MinSwingSpeed;
                swing.maxSpeed  = stats.MaxSwingSpeed;
            }
        }
    }

    /// <summary>
    /// Returns true if the given item is currently equipped.
    /// </summary>
    public bool IsEquipped(IInventoryItem item)
    {
        return item != null && item == currentItem;
    }
}
