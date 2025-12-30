using UnityEngine;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;
    [SerializeField] private InventorySlot[] slots;
    private int activeSlotIndex = 0;

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
        // Тепер тільки ОДНЕ місце вирішує, що робити з предметом
        PlayerController.Instance.EquipItem(item);
    }

    public bool AddItem(Item item)
    {
        // 1. Спроба стаку
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.CanStack(item))
                {
                    // Помилка CS1061: замінюємо AddOne() на AddItem з поточною кількістю + 1
                    slot.AddItem(item, slot.GetCurrentCount() + 1); 
                    if (slot.slotIndex == activeSlotIndex) UpdateActiveItem();
                    return true;
                }
            }
        }

        // 2. Пошук вільного місця
        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
            {
                // Помилка CS7036: додаємо другий аргумент (кількість = 1)
                slot.AddItem(item, 1); 
                if (slot.slotIndex == activeSlotIndex) UpdateActiveItem();
                return true;
            }
        }
        return false;
    }

    // Універсальний пошук кількості (для крафту або стріл)
    public int GetTotalCount(ItemType type)
    {
        int count = 0;
        foreach (var slot in slots)
            if (!slot.IsEmpty() && slot.GetItem().itemType == type) count += slot.GetCurrentCount();
        return count;
    }

    public bool RemoveItemsByType(ItemType type, int amount)
    {
        if (GetTotalCount(type) < amount) return false;
        
        int toRemove = amount;
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
        return true;
    }
    
    public int GetActiveSlotIndex() => activeSlotIndex;
    public void SetCurrentTool(string toolName) 
    {

    }
    
    public bool HasItemOfType(ItemType type)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.GetItem().itemType == type)
                return true;
        }
        return false;
    }

// Цей метод ми вже писали, але переконайся, що він виглядає саме так і він public
    public bool RemoveItemByType(ItemType type, int amount)
    {
        if (GetTotalCount(type) < amount) return false;
    
        int toRemove = amount;
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
        return true;
    }
}