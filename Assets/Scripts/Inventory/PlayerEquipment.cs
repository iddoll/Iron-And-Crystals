using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance;

    [Header("Character Renderers")]
    public SpriteRenderer helmetRenderer; // Спрайт шолома на голові

    private void Awake()
    {
        Instance = this;
    }

    public void EquipItem(Item item)
    {
        switch (item.itemType)
        {
            case ItemType.Helmet:
                helmetRenderer.sprite = item.equippedSprite;
                helmetRenderer.enabled = true;
                break;
        }
    }

    public void UnequipItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Helmet:
                helmetRenderer.sprite = null;
                helmetRenderer.enabled = false;
                break;
        }
    }
}