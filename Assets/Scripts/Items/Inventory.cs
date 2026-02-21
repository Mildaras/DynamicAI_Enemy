using UnityEngine;
using System.Collections.Generic;
using System;

public class Inventory : MonoBehaviour
{
    private const int SLOTS = 5;
    public HotbarSlot[] hotbarSlots;
    private List<IInventoryItem> mItems = new List<IInventoryItem>();
    public WeaponManager weaponManager; // Assign in inspector
    private IInventoryItem selectedItem;

    public event EventHandler<InventoryEventArgs> ItemAdded;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectHotbarSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectHotbarSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectHotbarSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectHotbarSlot(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SelectHotbarSlot(4);
    }

    public void AddItem(IInventoryItem item)
    {
        if (mItems.Count < SLOTS)
        {
            mItems.Add(item);
            item.OnPickup();

            int slotIndex = mItems.Count - 1;

            if (slotIndex >= 0 && slotIndex < hotbarSlots.Length && hotbarSlots[slotIndex] != null)
            {
                hotbarSlots[slotIndex].SetItem(item);
            }
            else
            {
                Debug.LogError($"Hotbar slot at index {slotIndex} is not assigned or out of bounds.");
            }

            ItemAdded?.Invoke(this, new InventoryEventArgs(item));
        }
        else
        {
            Debug.Log("Inventory is full.");
        }
    }

    void RefreshHotbar()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (i < mItems.Count)
                hotbarSlots[i].SetItem(mItems[i]);
            else
                hotbarSlots[i].ClearItem();
        }
    }

    public IInventoryItem FindItemByName(string itemName)
    {
        return mItems.Find(item => item.Name == itemName);
    }

    public bool Contains(IInventoryItem item)
    {
        //priint all items in the inventory
        foreach (var i in mItems)
        {
            Debug.Log(i.Name);
        }
        return mItems.Contains(item);
    }

    void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        selectedItem = hotbarSlots[index].GetItem();
        weaponManager.EquipWeapon(selectedItem);
    }

    public void RemoveItem(IInventoryItem item)
    {
        if (!mItems.Contains(item)) return;

        int idx = mItems.IndexOf(item);
        mItems.RemoveAt(idx);
        hotbarSlots[idx].ClearItem();
        RefreshHotbar();

        // if that was our equipped weapon, clear it
        if (weaponManager.IsEquipped(item))
            weaponManager.EquipWeapon(null);
    }

}
