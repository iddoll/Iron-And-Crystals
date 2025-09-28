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

            // 🛡️ ТЕПЕР ЛОГІКА ЩИТА ПОВНІСТЮ ТУТ! 🛡️
            case ItemType.Shield:
                if (PlayerController.Instance != null)
                {
                    // 1. Спочатку знімаємо попередній щит
                    PlayerController.Instance.UnequipShield(); 
                
                    // 2. Встановлюємо новий щит
                    if (item.equippedPrefab != null && PlayerController.Instance.shieldPoint != null)
                    {
                        GameObject heldShieldObject = Instantiate(item.equippedPrefab, PlayerController.Instance.shieldPoint.position, Quaternion.identity, PlayerController.Instance.shieldPoint);
                        heldShieldObject.transform.localPosition = Vector3.zero;
                        heldShieldObject.transform.localRotation = Quaternion.identity;
                    
                        Rigidbody2D rbHeld = heldShieldObject.GetComponent<Rigidbody2D>();
                        if (rbHeld != null)
                        {
                            rbHeld.simulated = false;
                            rbHeld.isKinematic = true;
                        }
                        // Важливо: ПРИЗНАЧИТИ heldShieldObject до PlayerController.Instance
                        PlayerController.Instance.heldShieldObject = heldShieldObject; 
                    }
                    ShieldController.Instance?.SetEquipped(true);
                }
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
    
        // 🛡️ Спеціальна обробка зняття щита 🛡️
        if (type == ItemType.Shield)
        {
            PlayerController.Instance?.UnequipShield();
        }
    }
}
