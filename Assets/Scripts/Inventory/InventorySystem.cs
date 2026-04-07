using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;
    
    [SerializeField] private InventorySlot[] slots;
    private int activeSlotIndex = 0;

    [Header("Special Slots")]
    // Змінено тип на EquipmentSlot, щоб він бачив твій слот для стріл
    public EquipmentSlot arrowSlot; 
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < slots.Length; i++)
            slots[i].slotIndex = i;
    }

    public InventorySlot GetActiveSlot() => slots[activeSlotIndex];

    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;
        activeSlotIndex = index;
        UpdateActiveItem();
    }

    public void UpdateActiveItem()
    {
        Item item = GetActiveSlot()?.GetItem();
        PlayerController.Instance.EquipItem(item);
    }

    public bool AddItem(Item item)
    {
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.CanStack(item))
                {
                    slot.AddItem(item, slot.GetCurrentCount() + 1); 
                    if (slot.slotIndex == activeSlotIndex) UpdateActiveItem();
                    return true;
                }
            }
        }

        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
            {
                slot.AddItem(item, 1); 
                if (slot.slotIndex == activeSlotIndex) UpdateActiveItem();
                return true;
            }
        }
        return false;
    }

    // Додаємо підтримку додавання пачки предметів (наприклад, для повернення з екіпіровки)
    public bool AddItemWithCount(Item item, int amount)
    {
        // 1. Спроба додати в існуючі стаки
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.CanStack(item))
                {
                    int canAdd = Mathf.Min(amount, item.maxStack - slot.GetCurrentCount());
                    slot.AddItem(item, slot.GetCurrentCount() + canAdd);
                    amount -= canAdd;
                    if (amount <= 0) return true;
                }
            }
        }

        // 2. Додавання залишку в нові слоти
        while (amount > 0)
        {
            InventorySlot emptySlot = null;
            foreach (var slot in slots)
            {
                if (slot.IsEmpty()) { emptySlot = slot; break; }
            }

            if (emptySlot != null)
            {
                int toAdd = Mathf.Min(amount, item.maxStack);
                emptySlot.AddItem(item, toAdd);
                amount -= toAdd;
            }
            else return false; // Інвентар повний
        }
        return true;
    }

    public int GetTotalCount(ItemType type)
    {
        int count = 0;
        // Додаємо кількість зі спец-слота, якщо тип збігається
        if (type == ItemType.Arrow && arrowSlot != null && arrowSlot.GetItem() != null)
        {
            count += arrowSlot.GetCount();
        }

        foreach (var slot in slots)
            if (!slot.IsEmpty() && slot.GetItem().itemType == type) count += slot.GetCurrentCount();
        return count;
    }

    public bool HasItemOfType(ItemType type)
    {
        // Перевірка спец-слота
        if (type == ItemType.Arrow && arrowSlot != null && arrowSlot.GetItem() != null) return true;

        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.GetItem().itemType == type)
                return true;
        }
        return false;
    }

    public bool RemoveItemByType(ItemType type, int amount)
    {
        if (GetTotalCount(type) < amount) return false;
    
        int toRemove = amount;

        // 1. Спочатку видаляємо зі спец-слота, якщо ми шукаємо стріли
        if (type == ItemType.Arrow && arrowSlot != null && arrowSlot.GetItem() != null)
        {
            int inSlot = arrowSlot.GetCount();
            int take = Mathf.Min(toRemove, inSlot);
            
            if (inSlot > take)
                arrowSlot.SetItem(arrowSlot.GetItem(), inSlot - take);
            else
                arrowSlot.ClearSlotVisuals();

            toRemove -= take;
        }

        // 2. Потім з решти інвентарю
        if (toRemove > 0)
        {
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty() && slot.GetItem().itemType == type)
                {
                    int canTake = Mathf.Min(toRemove, slot.GetCurrentCount());
                    slot.ReduceStack(canTake);
                    toRemove -= canTake;
                    if (slot == GetActiveSlot()) UpdateActiveItem();
                    if (toRemove <= 0) break;
                }
            }
        }
        return true;
    }

    public Item GetAmmoToUse(out int displayCount)
    {
        displayCount = 0;

        // 1. Пріоритет: Спеціальний EquipmentSlot
        if (arrowSlot != null && arrowSlot.GetItem() != null)
        {
            displayCount = arrowSlot.GetCount();
            return arrowSlot.GetItem();
        }

        // 2. Якщо пусто: Загальна кількість всіх стріл в інвентарі
        Item firstFoundArrow = null;
        int totalInInventory = 0;

        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.GetItem().itemType == ItemType.Arrow)
            {
                if (firstFoundArrow == null) firstFoundArrow = slot.GetItem();
                totalInInventory += slot.GetCurrentCount();
            }
        }

        displayCount = totalInInventory;
        return firstFoundArrow;
    }

    public void ConsumeArrow()
    {
        // Використовуємо наш універсальний метод видалення
        RemoveItemByType(ItemType.Arrow, 1);
    }

    public int GetActiveSlotIndex() => activeSlotIndex;
}