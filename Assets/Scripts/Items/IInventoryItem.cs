using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;


public interface IInventoryItem
{
    string Name { get; }
    string Description { get; }
    Sprite Image { get; }
    GameObject Prefab { get; }

    void OnPickup();
}

public class InventoryEventArgs : EventArgs
{
    public InventoryEventArgs(IInventoryItem item)
    {
        Item = item;
    }
    public IInventoryItem Item;
}
