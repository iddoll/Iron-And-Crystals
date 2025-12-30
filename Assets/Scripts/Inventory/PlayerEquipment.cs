using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance;
    public SpriteRenderer helmetRenderer;

    private void Awake() => Instance = this;

    public void EquipSlotItem(Item item)
    {
        if (item == null) return;

        switch (item.itemType)
        {
            case ItemType.Helmet:
                helmetRenderer.sprite = item.equippedSprite;
                helmetRenderer.enabled = true;
                break;

            case ItemType.Shield:
                // Викликаємо PlayerController, щоб він створив фізичний об'єкт щита
                PlayerController.Instance.SetupShield(item);
                ShieldController.Instance?.SetEquipped(true);
                break;
        }
    }

    public void UnequipSlotItem(ItemType type)
    {
        if (type == ItemType.Helmet)
        {
            helmetRenderer.sprite = null;
            helmetRenderer.enabled = false;
        }
        else if (type == ItemType.Shield)
        {
            PlayerController.Instance.UnequipShield();
            ShieldController.Instance?.SetEquipped(false);
        }
    }
}