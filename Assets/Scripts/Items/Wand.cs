using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Wand : MonoBehaviour, IInventoryItem
{
    public string Name => "Wand";
    public string Description => "Hold and you will see";
    [SerializeField] private Sprite _image;
    [SerializeField] private GameObject _itemPrefab; // Assuming you have a prefab for the other
    public Sprite Image => _image;
    public GameObject Prefab => _itemPrefab; // Assuming the prefab is the same as this script's GameObject

    void Awake()
    {
        _image = Resources.Load<Sprite>("Sprites/Wand"); //In Srites folder needs to be named accordingly
        _itemPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Wand"); // Ensure correct prefab path
    }

    public void OnPickup()
    {
        Debug.Log("Wand picked up");
        Debug.Log(Image);
    }
}