using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    public Image itemImage;
    private IInventoryItem currentItem;

    public void SetItem(IInventoryItem item)
    {
        currentItem = item;
        itemImage.sprite = item.Image;
        itemImage.enabled = true;
    }

    public void ClearItem()
    {
        currentItem = null;
        itemImage.sprite = null;
        itemImage.enabled = false;
    }

    public IInventoryItem GetItem()
    {
        return currentItem;
    }
}
