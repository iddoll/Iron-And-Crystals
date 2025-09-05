using UnityEngine;

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

    public int GetActiveSlotIndex() => activeSlotIndex;

    public InventorySlot GetActiveSlot() => (activeSlotIndex >= 0 && activeSlotIndex < slots.Length) ? slots[activeSlotIndex] : null;

    /// <summary>Встановлює активний слот. Якщо слот не змінюється, просто оновлює екіпірування.</summary>
    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        if (activeSlotIndex != index)
            activeSlotIndex = index;

        UpdateActiveItem();
    }

    /// <summary>Оновлює екіпірування гравця відповідно до предмета в активному слоті.</summary>
    public void UpdateActiveItem()
    {
        InventorySlot slot = GetActiveSlot();
        Item item = slot?.GetItem();

        if (item != null && item.equippedPrefab != null)
            PlayerController.Instance.EquipItem(item);
        else
            PlayerController.Instance.UnequipItem();
    }

    /// <summary>Додає предмет в інвентар або в стек, якщо він стековий.</summary>
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

                // Якщо доданий предмет у активний слот — оновлюємо анімацію
                if (i == activeSlotIndex || GetActiveSlot().IsEmpty())
                {
                    SetActiveSlot(i); // робимо слот активним якщо активний порожній
                }

                return true;
            }
        }

        Debug.Log("Інвентар повний!");
        return false;
    }

    /// <summary>Видаляє предмет повністю або зі стека.</summary>
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

                // Якщо видалили предмет з активного слоту — оновлюємо екіпірування
                if (slot == GetActiveSlot())
                    UpdateActiveItem();

                return;
            }
        }
        Debug.LogWarning($"Предмет {item.itemName} не знайдено в інвентарі для видалення.");
    }

    /// <summary>Видаляє задану кількість предметів зі стеків.</summary>
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

    /// <summary>Повертає кількість предметів у всіх слотах.</summary>
    public int GetItemCount(Item item)
    {
        int count = 0;
        foreach (var slot in slots)
            if (slot.GetItem() == item)
                count += slot.GetCurrentCount();
        return count;
    }

    /// <summary>Встановлює предмет активним за іменем.</summary>
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
}
