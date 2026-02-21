using UnityEngine;

public class Other : MonoBehaviour, IInventoryItem
{
    public string Name => "Staff";
    public string Description => "A other for combat";
    [SerializeField] private Sprite _image;
    [SerializeField] private GameObject _itemPrefab; // Assuming you have a prefab for the other
    public Sprite Image => _image;
    public GameObject Prefab => _itemPrefab; // Assuming the prefab is the same as this script's GameObject

    void Awake()
    {
        _image = Resources.Load<Sprite>("Sprites/Staff"); //In Srites folder needs to be named accordingly
        _itemPrefab = Resources.Load<GameObject>("Prefabs/Weapons/Staff"); // Ensure correct prefab path
    }

    public void OnPickup()
    {
        Debug.Log("Other picked up");
        Debug.Log(Image);
    }
}
