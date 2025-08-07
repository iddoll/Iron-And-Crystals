using UnityEngine;

public enum ItemType
{
    None,
    Pickaxe,
    Sword,
    Axe,
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

    public GameObject worldPrefab; // предмет у світі (із фізикою)

    public GameObject equippedPrefab; // предмет у руці (без фізики)

    public ItemType itemType;

    public bool canBeEquipped = false;
    public Vector3 equippedLocalPosition = Vector3.zero;
    public Quaternion equippedLocalRotation = Quaternion.identity;

    // НОВЕ ПОЛЕ: Визначає, як предмет підбирається
    public PickupMethod pickupMethod = PickupMethod.OnEPress; // За замовчуванням 'E'
}