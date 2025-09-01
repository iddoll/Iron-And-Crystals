using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;
    [SerializeField] private InventorySlot[] slots;
    private int activeSlotIndex = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool AddItem(Item item)
    {
        Debug.Log($"[InventorySystem] AddItem викликано для {item.itemName}"); // Додано лог для діагностики

        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.CanStack(item) && slot.GetCurrentCount() < item.maxStack)
                {
                    slot.AddOne();
                    Debug.Log($"Предмет {item.itemName} додано до стаку. Кількість: {slot.GetCurrentCount()}");
                    return true;
                }
            }
        }

        // Шукаємо порожній слот
        for (int i = 0; i < slots.Length; i++) // Перебираємо всі слоти
        {
            if (slots[i].IsEmpty())
            {
                slots[i].AddItem(item); // Додаємо предмет в слот
                Debug.Log($"Предмет {item.itemName} додано в слот {i}."); // Додано лог

                // Логіка активації слота та екіпіровки
                // Якщо це перший предмет в інвентарі, або якщо додається предмет, який може бути екіпірований,
                // І це наш перший екіпірований предмет, або слот, куди додали, стає активним
                if (activeSlotIndex == -1) // Якщо інвентар був порожній і це перший доданий предмет
                {
                    SetActiveSlot(i); // Активація першого слота
                }
                else if (activeSlotIndex == i && item.equippedPrefab != null) // Якщо предмет додано в активний слот, і він може бути екіпірований
                {
                    SetActiveSlot(i); // Переактивовуємо слот, щоб екіпірувати новий предмет
                }
                
                return true;
            }
        }

        Debug.Log("Інвентар повний!");
        return false;
    }
    
    public void SetActiveSlot(int index)
    {
        Debug.Log($"[InventorySystem] SetActiveSlot викликано для індексу {index}. Поточний activeSlotIndex: {activeSlotIndex}"); // Додано лог
        
        if (index >= 0 && index < slots.Length)
        {
            // Якщо ми перемикаємося на той самий слот і він вже активний, нічого не робимо
            // Ця перевірка важлива, щоб уникнути зайвих Unequip/Equip
            if (activeSlotIndex == index && slots[activeSlotIndex].GetItem() != null && PlayerController.Instance.GetCurrentTool() == slots[activeSlotIndex].GetItem().name)
            {
                Debug.Log($"[InventorySystem] Слот {index} вже активний з тим самим предметом. Нічого не робимо.");
                return;
            }

            // Де-екіпіруємо поточний предмет, якщо він є і він був у активному слоті
            if (activeSlotIndex != -1)
            {
                Item prevItem = slots[activeSlotIndex].GetItem();
                if (prevItem != null && prevItem.equippedPrefab != null)
                {
                    PlayerController.Instance.UnequipItem();
                }
            }

            activeSlotIndex = index;
            Item item = slots[index].GetItem();

            if (item != null && item.equippedPrefab != null)
            {
                PlayerController.Instance.EquipItem(item);
                Debug.Log("Слот " + index + " активовано. Предмет: " + item.itemName);
            }
            else
            {
                // Якщо предмет не можна екіпірувати (наприклад, кристал),
                // просто де-екіпіруємо поточний і не екіпіруємо нічого нового.
                PlayerController.Instance.UnequipItem();
                PlayerController.Instance.SetCurrentTool("None");
                Debug.Log("Слот " + index + " активовано. Предмет не екіпіровано (немає equippedPrefab).");
            }
        }
    }

   public void RemoveItem(Item item)
    {
        // ... (ваш існуючий код RemoveItem) ...
        foreach (var slot in slots) //
        {
            if (slot.GetItem() == item) //
            {
                if (item.isStackable && slot.GetCurrentCount() > 1) //
                {
                    slot.RemoveOne(); //
                    Debug.Log($"Один {item.itemName} видалено зі стаку."); //
                    return; //
                }
                else //
                {
                    slot.ClearSlot(); //
                    Debug.Log($"{item.itemName} повністю видалено зі слота."); //

                    if (slot == slots[activeSlotIndex]) //
                    {
                        PlayerController.Instance.UnequipItem(); //
                        PlayerController.Instance.SetCurrentTool("None"); //
                    }
                    return; //
                }
            }
        }
        Debug.Log($"Предмет {item.itemName} не знайдено в інвентарі для видалення."); //
    }

    public bool RemoveItems(Item item, int amount)
    {
        if (amount <= 0) return true; //

        int itemsRemoved = 0; //
        for (int i = 0; i < slots.Length; i++) //
        {
            var slot = slots[i]; //
            if (slot.GetItem() == item) //
            {
                int itemsToTake = Mathf.Min(amount - itemsRemoved, slot.GetCurrentCount()); //
                for (int j = 0; j < itemsToTake; j++) //
                {
                    slot.RemoveOne(); //
                    itemsRemoved++; //
                }

                if (slot.IsEmpty() && i == activeSlotIndex) //
                {
                    PlayerController.Instance.UnequipItem(); //
                    PlayerController.Instance.SetCurrentTool("None"); //
                }

                if (itemsRemoved >= amount) //
                {
                    Debug.Log($"Видалено {amount} {item.itemName} з інвентарю."); //
                    return true; //
                }
            }
        }

        Debug.LogWarning($"Не вдалося видалити {amount} {item.itemName}. Знайдено лише {itemsRemoved}."); //
        return false; //
    }

    public int GetItemCount(Item item)
    {
        // ... (ваш існуючий код GetItemCount) ...
        int count = 0; //
        foreach (var slot in slots) //
        {
            if (slot.GetItem() == item) //
            {
                count += slot.GetCurrentCount(); //
            }
        }
        return count; //
    }

    public InventorySlot GetActiveSlot()
    {
        if (activeSlotIndex != -1 && activeSlotIndex < slots.Length)
        {
            return slots[activeSlotIndex];
        }
        return null;
    }
}