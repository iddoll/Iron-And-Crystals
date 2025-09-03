using UnityEngine;

public enum ItemType
{
    None,
    Pickaxe,
    Sword,
    Axe,
    Lance,
    Bow,
    Arrow,
    Crystal
}

// Нове перерахування для методів підбору
public enum PickupMethod
{
    OnTouch, // Підбирається при дотику (для кристалів, ресурсів)
    OnEPress // Підбирається при натисканні 'E' (для інструментів, зброї)
}

[CreateAssetMenu(menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public bool isStackable;
    public int maxStack = 1;
    
    public AnimatorOverrideController overrideController;
    
    [Header("Combat Settings")]
    public float damage = 10f;
    public float attackCooldown = 1f;
    
    public GameObject worldPrefab; // предмет у світі (із фізикою)

    public GameObject equippedPrefab; // предмет у руці (без фізики)

    public ItemType itemType;

    public bool canBeEquipped = false;
    public Vector3 equippedLocalPosition = Vector3.zero;
    public Quaternion equippedLocalRotation = Quaternion.identity;

    // НОВЕ ПОЛЕ: Визначає, як предмет підбирається
    public PickupMethod pickupMethod = PickupMethod.OnEPress; // За замовчуванням 'E'
}