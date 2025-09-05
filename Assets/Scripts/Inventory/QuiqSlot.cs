using UnityEngine;

public class QuiqSlot : MonoBehaviour
{
    public static QuiqSlot Instance;

    [SerializeField] private InventorySlot[] quickSlots;
    // [SerializeField] private WeaponHold weaponHold; // <-- ВИДАЛИТИ ЦЕЙ РЯДОК

    private int currentQuickIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        for (int i = 0; i < 5; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                // Цей метод вже викликає InventorySystem.Instance.SetActiveSlot
                // Тому, ви просто викликаєте SetActiveSlot в InventorySystem
                InventorySystem.Instance.SetActiveSlot(i); // <-- ЗМІНА ТУТ
                // Більше не потрібно викликати UpdateHeldItem тут напряму
            }
        }
    }

    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= quickSlots.Length) return;

        // Якщо індекс той самий, що й поточний, і вже оброблено,
        // ми можемо додати перевірку, щоб уникнути зайвих викликів.
        if (currentQuickIndex == index)
        {
            // Якщо вам потрібно примусово оновити, навіть якщо слот той самий,
            // то залишіть виклик SetActiveSlot, але тоді InventorySystem.SetActiveSlot
            // має бути стійким до повторних викликів. (Ми вже це додали)
        }

        currentQuickIndex = index;

        // Цей метод викликається ззовні (наприклад, з UI)
        // Він має передати індекс в InventorySystem, а не керувати WeaponHold.
        InventorySystem.Instance.SetActiveSlot(index); // <-- ЗМІНА ТУТ: передаємо в InventorySystem

        // weaponHold.Equip/Unequip більше не потрібні тут
        // UpdateHeldItem(); // <-- ВИДАЛИТИ ЦЕЙ РЯДОК
    }

    // Цей метод більше не потрібен у QuiqSlot, тому що InventorySystem вже робить це.
    // Public метод UpdateHeldItem() є надлишковим.
    /*
    public void UpdateHeldItem() // <-- ВИДАЛИТИ АБО ЗАКОМЕНТУВАТИ ВЕСЬ ЦЕЙ МЕТОД
    {
        Item item = quickSlots[currentQuickIndex].GetItem();

        if (item != null)
        {
            weaponHold.Equip(item);
        }
        else
        {
            weaponHold.Unequip();
        }
    }
    */

    public bool TryAddItemToQuickSlot(Item item)
    {
        for (int i = 0; i < 5; i++)
        {
            if (quickSlots[i].IsEmpty())
            {
                quickSlots[i].AddItem(item);
                // Коли предмет додається в слот, потрібно також повідомити InventorySystem,
                // щоб він активував цей слот, якщо потрібно.
                // InventorySystem.Instance.SetActiveSlot(i); // <-- Цей виклик вже відбувається в AddItem в InventorySystem
                                                             // Якщо ви хочете, щоб слот одразу ставав активним при додаванні сюди,
                                                             // переконайтеся, що це не дублює логіку AddItem.
                                                             // Найкраще, щоб InventorySystem.AddItem сам вирішував, який слот активувати.
                return true;
            }
        }

        return false;
    }

    public bool IsQuickSlot(InventorySlot slot)
    {
        foreach (InventorySlot s in quickSlots)
        {
            if (s == slot) return true;
        }
        return false;
    }

    public bool IsActiveSlot(InventorySlot slot)
    {
        return quickSlots[currentQuickIndex] == slot;
    }

    public void OnSlotChanged(InventorySlot changedSlot)
    {
        // Якщо змінений слот є активним, оновлюємо екіпірування через InventorySystem
        if (IsActiveSlot(changedSlot))
        {
            InventorySystem.Instance.UpdateActiveItem();
        }
    }

}