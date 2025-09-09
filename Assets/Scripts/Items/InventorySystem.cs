using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    [SerializeField] private InventorySlot[] slots;
    private int activeSlotIndex = 0;
    
    // Масив для всіх Item Scriptable Objects
    public Item[] allItems;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < slots.Length; i++)
            slots[i].slotIndex = i;
    }

    public int GetActiveSlotIndex() => activeSlotIndex;

    public InventorySlot GetActiveSlot() => (activeSlotIndex >= 0 && activeSlotIndex < slots.Length) ? slots[activeSlotIndex] : null;

    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        if (activeSlotIndex != index)
            activeSlotIndex = index;

        UpdateActiveItem();
    }

    public void UpdateActiveItem()
    {
        InventorySlot slot = GetActiveSlot();
        Item item = slot?.GetItem();

        if (item != null && item.equippedPrefab != null)
            PlayerController.Instance.EquipItem(item);
        else
            PlayerController.Instance.UnequipItem();
    }

    public bool AddItem(Item item)
    {
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.CanStack(item) && slot.GetCurrentCount() < item.maxStack)
                {
                    slot.AddOne();
                    if (slot == GetActiveSlot())
                        UpdateActiveItem();
                    return true;
                }
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty())
            {
                slots[i].AddItem(item);
                if (i == activeSlotIndex || GetActiveSlot().IsEmpty())
                {
                    SetActiveSlot(i);
                }
                return true;
            }
        }

        Debug.Log("Інвентар повний!");
        return false;
    }

    public void RemoveItem(Item item)
    {
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item)
            {
                if (item.isStackable && slot.GetCurrentCount() > 1)
                {
                    slot.RemoveOne();
                }
                else
                {
                    slot.ClearSlot();
                }
                if (slot == GetActiveSlot())
                    UpdateActiveItem();
                return;
            }
        }
        Debug.LogWarning($"Предмет {item.itemName} не знайдено в інвентарі для видалення.");
    }

    public bool RemoveItems(Item item, int amount)
    {
        if (amount <= 0) return true;
        int removed = 0;
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item)
            {
                int toRemove = Mathf.Min(amount - removed, slot.GetCurrentCount());
                for (int i = 0; i < toRemove; i++)
                    slot.RemoveOne();
                removed += toRemove;
                if (slot == GetActiveSlot())
                    UpdateActiveItem();
                if (removed >= amount)
                    return true;
            }
        }
        Debug.LogWarning($"Не вдалося видалити {amount} {item.itemName}. Видалено лише {removed}.");
        return false;
    }

    public int GetItemCount(Item item)
    {
        int count = 0;
        foreach (var slot in slots)
            if (slot.GetItem() == item)
                count += slot.GetCurrentCount();
        return count;
    }

    public void SetCurrentTool(string toolName)
    {
        if (toolName == "None")
        {
            PlayerController.Instance.UnequipItem();
            return;
        }
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty() && slots[i].GetItem().name == toolName)
            {
                SetActiveSlot(i);
                return;
            }
        }
        Debug.LogWarning($"SetCurrentTool: предмет з іменем {toolName} не знайдено в слотах.");
    }
    
    public bool HasItemOfType(ItemType type)
    {
        foreach (InventorySlot slot in slots) 
        {
            if (!slot.IsEmpty() && slot.GetItem().itemType == type)
            {
                return true;
            }
        }
        return false;
    }

    public void AddItem(ItemType type, int amount)
    {
        Item itemToAdd = null;
        foreach (Item item in allItems)
        {
            if (item.itemType == type)
            {
                itemToAdd = item;
                break;
            }
        }
        
        if (itemToAdd == null)
        {
            Debug.LogError($"[InventorySystem] Item of type {type} not found in allItems array!");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            if (!AddItem(itemToAdd))
            {
                Debug.LogWarning($"[InventorySystem] Inventory is full, could not add all {amount} items.");
                return;
            }
        }
    }

    public bool RemoveItemByType(ItemType type, int amount)
    {
        if (amount <= 0) return true;
        int totalCount = 0;
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.GetItem().itemType == type)
            {
                totalCount += slot.GetCurrentCount();
            }
        }
        if (totalCount < amount)
        {
            Debug.LogWarning($"[InventorySystem] Not enough items of type {type} to remove. Needed: {amount}, Have: {totalCount}.");
            return false;
        }
        int removedCount = 0;
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty() && slot.GetItem().itemType == type)
            {
                int toRemoveFromThisSlot = Mathf.Min(amount - removedCount, slot.GetCurrentCount());
                slot.ReduceStack(toRemoveFromThisSlot);
                removedCount += toRemoveFromThisSlot;
                if (slot == GetActiveSlot())
                    UpdateActiveItem();
                if (removedCount >= amount)
                {
                    return true;
                }
            }
        }
        return false;
    }
}