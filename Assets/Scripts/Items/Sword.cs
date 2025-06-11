// Sword.cs
using UnityEngine;

public class Sword : MonoBehaviour, IInventoryItem, IWeaponStats
{
    // IInventoryItem
    public string Name => "Sword";
    public string Description => "A finely-balanced blade.";
    public Sprite Image => _image;
    public GameObject Prefab => _itemPrefab;

    [Header("Stats")]
    [SerializeField] private float _damage = 100f;
    [SerializeField] private float _minSpeed = 300f;   // pixels per second
    [SerializeField] private float _maxSpeed = 1500f;

    // IWeaponStats
    public float Damage => _damage;
    public float MinSwingSpeed => _minSpeed;
    public float MaxSwingSpeed => _maxSpeed;

    [SerializeField] private Sprite _image;
    [SerializeField] private GameObject _itemPrefab;

    void Awake()
    {
        _image = Resources.Load<Sprite>("Sprites/Sword");
        _itemPrefab = Resources.Load<GameObject>("Prefabs/Weapons/SwordPrefab");
    }

    public void OnPickup()
    {
        Debug.Log("Picked up a sword.");
    }
}
