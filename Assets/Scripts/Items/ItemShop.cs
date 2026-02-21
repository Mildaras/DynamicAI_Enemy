using UnityEngine;

public class ItemShop : MonoBehaviour
{
    private Inventory inventory;
    public GameObject NPC;
    public GameObject buttons;
    //private HotbarSlot hotbarSlot = new HotbarSlot();

    void Start()
    {
        inventory = FindObjectOfType<Inventory>();
        //hotbarSlot = FindObjectOfType<HotbarSlot>();
    }
    public void buySword()
    {
        IInventoryItem item = new Sword();
        if(PlayerData.purchaseFromNPC(0))
        {
            Sword sword = new GameObject("Sword").AddComponent<Sword>();
            Transform button = buttons.transform.Find("Sword");
            sword.gameObject.SetActive(false);
            button.gameObject.SetActive(false);

            inventory.AddItem(sword);
        }

    }

    public void buyOther()
    {
        IInventoryItem item = new Other();
        if (PlayerData.purchaseFromNPC(3000))
        {
            Other other = new GameObject("Staff").AddComponent<Other>();
            Transform button = buttons.transform.Find("Other");
            button.gameObject.SetActive(false);
            other.gameObject.SetActive(false);
            inventory.AddItem(other);
        }
    }
    public void buyWand()
    {
        IInventoryItem item = new Wand();
        if (PlayerData.purchaseFromNPC(3001))
        {
            Wand wand = new GameObject("Wand").AddComponent<Wand>();
            Transform button = buttons.transform.Find("Wand");
            button.gameObject.SetActive(false);
            wand.gameObject.SetActive(false);
            inventory.AddItem(wand);
        }
    }
}
