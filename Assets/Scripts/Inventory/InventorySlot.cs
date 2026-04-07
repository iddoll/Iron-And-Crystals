using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image icon;
    [SerializeField] private Text countText;

    [Header("Slot Settings")]
    public ItemType allowedType = ItemType.None; // None = будь-який предмет
    
    private Item currentItem;
    private int count;
    public int slotIndex;

    // --- DRAG LOGIC ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty()) return;

        // Починаємо перетягування
        InventoryDragManager.Instance.StartDragging(this, currentItem, count, icon.sprite);
        
        // ВАЖЛИВО: Очищаємо слот ТІЛЬКИ після того, як менеджер отримав дані.
        // Це гарантує, що "0" зникне одразу, як ти підняв предмет мишкою.
        ClearSlot();
    }

    public void OnDrag(PointerEventData eventData)
    {
        InventoryDragManager.Instance.UpdateDraggedPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Якщо предмет не знайшов куди "приземлитися", менеджер сам поверне його або викине
        InventoryDragManager.Instance.OnEndDrag(eventData, this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryDragManager.Instance.HasItem()) return;

        Item incomingItem = InventoryDragManager.Instance.GetItem();
        int incomingCount = InventoryDragManager.Instance.GetCount();

        // --- ПЕРЕВІРКА ТИПУ (Виправлення зникнення) ---
        if (allowedType != ItemType.None && incomingItem.itemType != allowedType)
        {
            // Якщо тип не підходить, ми просто нічого не робимо. 
            // InventoryDragManager.OnEndDrag сам поверне предмет у початковий слот.
            return;
        }

        InventorySlot fromSlot = InventoryDragManager.Instance.GetSourceSlot();
        // Примітка: якщо у тебе є окремий клас EquipmentSlot, переконайся, що він теж працює через цей менеджер

        // 1. Спроба стакування
        if (!IsEmpty() && CanStack(incomingItem))
        {
            int spaceLeft = currentItem.maxStack - count;
            int amountToStack = Mathf.Min(spaceLeft, incomingCount);
            
            count += amountToStack;
            RefreshUI();

            if (incomingCount > amountToStack)
                InventoryDragManager.Instance.StartDragging(fromSlot, incomingItem, incomingCount - amountToStack, incomingItem.icon);
            else
                InventoryDragManager.Instance.StopDragging();

            return;
        }

        // 2. Обмін (Swap)
        Item tempItem = currentItem;
        int tempCount = count;

        // ВАЖЛИВО: перевіряємо, чи можна повернути старий предмет у початковий слот
        // (наприклад, якщо ми міняємо шолом на шолом у слоті екіпіровки)
        if (fromSlot != null && tempItem != null)
        {
            if (fromSlot.allowedType != ItemType.None && tempItem.itemType != fromSlot.allowedType)
            {
                // Якщо старий предмет не влізе в той слот, звідки прийшов новий - скасовуємо обмін
                return; 
            }
        }

        AddItem(incomingItem, incomingCount);

        if (fromSlot != null && fromSlot != this)
        {
            fromSlot.AddItem(tempItem, tempCount);
        }

        InventoryDragManager.Instance.StopDragging();
    }

    // --- CLICK LOGIC ---

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && !IsEmpty())
        {
            // Швидке викидання на ПКМ
            PlayerController.Instance.DropItemFromInventory(currentItem, 1);
            ReduceStack(1);
        }
    }

    // --- INTERNAL METHODS ---

    public void AddItem(Item item, int amount)
    {
        if (item == null) { ClearSlot(); return; }
        
        currentItem = item;
        count = amount;
        RefreshUI();

        if (InventorySystem.Instance.GetActiveSlotIndex() == slotIndex)
            InventorySystem.Instance.UpdateActiveItem();
    }

    public void ClearSlot()
    {
        currentItem = null;
        count = 0;
        RefreshUI();

        if (InventorySystem.Instance.GetActiveSlotIndex() == slotIndex)
            InventorySystem.Instance.UpdateActiveItem();
    }

    private void RefreshUI()
    {
        if (icon == null || countText == null) return;

        if (currentItem != null)
        {
            icon.sprite = currentItem.icon;
            icon.enabled = true;
            
            // --- ВИПРАВЛЕННЯ БАГУ СТРІЛ (Показуємо кількість завжди для Arrows) ---
            bool shouldShowCount = (currentItem.isStackable && count > 1) || currentItem.itemType == ItemType.Arrow;
            
            if (shouldShowCount)
            {
                countText.text = count.ToString();
                countText.enabled = true;
            }
            else
            {
                countText.enabled = false;
            }
        }
        else
        {
            icon.enabled = false;
            countText.enabled = false;
        }
    }

    // Додамо метод GetCurrentCount для менеджера
    public int GetCount() => count;

    public bool IsEmpty() => currentItem == null;
    public bool CanStack(Item item) => !IsEmpty() && currentItem == item && currentItem.isStackable && count < currentItem.maxStack;
    public Item GetItem() => currentItem;
    public int GetCurrentCount() => count;
    public void ReduceStack(int amount) { count -= amount; if (count <= 0) ClearSlot(); else RefreshUI(); }
}