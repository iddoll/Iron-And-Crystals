using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot Configuration")]
    public ItemType allowedType = ItemType.Helmet;
    
    [Header("UI References")]
    public Image icon;
    [SerializeField] private Text countText; // Перетягни сюди об'єкт тексту кількості

    private Item currentItem;
    private int currentCount; // Додана змінна для зберігання стаку (наприклад, стріл)

    // --- Логіка перетягування (Drag and Drop) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;
        
        // Передаємо реальну кількість (currentCount) замість одиниці
        InventoryDragManager.Instance.StartDragging(this, currentItem, currentCount, icon.sprite);
        
        // Повідомляємо систему екіпірування, що предмет знято
        if (PlayerEquipment.Instance != null)
            PlayerEquipment.Instance.UnequipSlotItem(currentItem.itemType);
            
        ClearSlotVisuals();
    }

    public void OnDrag(PointerEventData eventData)
    {
        InventoryDragManager.Instance.UpdateDraggedPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Менеджер перевірить, чи прийняв хтось предмет. Якщо ні — поверне сюди.
        InventoryDragManager.Instance.OnEndDrag(eventData, this);
    }

    // --- Логіка OnDrop ---

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryDragManager.Instance.HasItem()) return;

        Item droppedItem = InventoryDragManager.Instance.GetItem();
        int droppedCount = InventoryDragManager.Instance.GetCount(); // Отримуємо кількість з менеджера
        
        InventorySlot sourceSlot = InventoryDragManager.Instance.GetSourceSlot();
        EquipmentSlot sourceEquipmentSlot = InventoryDragManager.Instance.GetSourceEquipSlot();

        // Перевірка типу
        if (droppedItem == null || droppedItem.itemType != allowedType) return;
        if (sourceEquipmentSlot == this) return;
        
        // Зберігаємо поточний предмет для обміну (Swap)
        Item tempItem = currentItem;
        int tempCount = currentCount;
        
        // Встановлюємо новий предмет
        SetItem(droppedItem, droppedCount);
        
        // Повертаємо старий предмет туди, звідки прийшов новий
        if (sourceSlot != null)
        {
            if (tempItem != null) sourceSlot.AddItem(tempItem, tempCount);
            else sourceSlot.ClearSlot();
        }
        else if (sourceEquipmentSlot != null)
        {
            if (tempItem != null) sourceEquipmentSlot.SetItem(tempItem, tempCount);
            else sourceEquipmentSlot.ClearSlotVisuals();
        }
        
        InventoryDragManager.Instance.StopDragging();
    }

    // --- Внутрішні методи ---

    public void SetItem(Item newItem, int amount)
    {
        currentItem = newItem;
        currentCount = amount;

        if (icon != null)
        {
            icon.sprite = newItem.icon;
            icon.enabled = true;
        }

        RefreshUI();

        if (PlayerEquipment.Instance != null)
            PlayerEquipment.Instance.EquipSlotItem(newItem);
    }

    public void RefreshUI()
    {
        if (countText == null) return;

        if (currentItem != null)
        {
            // Показуємо цифру, якщо це стріли (навіть 1) або якщо стак більше 1
            bool shouldShowCount = currentItem.itemType == ItemType.Arrow || (currentItem.isStackable && currentCount > 1);
            
            countText.text = shouldShowCount ? currentCount.ToString() : "";
            countText.enabled = shouldShowCount;
        }
        else
        {
            countText.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            UnequipToInventory();
        }
    }

    public void UnequipToInventory()
    {
        if (currentItem == null) return;

        // Намагаємось додати в інвентар весь стак
        bool added = InventorySystem.Instance.AddItem(currentItem); 
        // Примітка: якщо твій AddItem не підтримує кількість, предмет може додатися лише в кількості 1.
        // Переконайся, що в InventorySystem.AddItem реалізована робота з сумою предметів.

        if (added)
        {
            if (PlayerEquipment.Instance != null)
                PlayerEquipment.Instance.UnequipSlotItem(currentItem.itemType);
            
            ClearSlotVisuals();
        }
        else
        {
            // Викидаємо у світ, якщо інвентар повний
            PlayerController.Instance.DropItemFromInventory(currentItem, currentCount);
            
            if (PlayerEquipment.Instance != null)
                PlayerEquipment.Instance.UnequipSlotItem(currentItem.itemType);
                
            ClearSlotVisuals();
        }
    }

    public void ClearSlotVisuals()
    {
        currentItem = null;
        currentCount = 0;
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
        if (countText != null) countText.enabled = false;
    }

    // Допоміжні методи для менеджера
    public Item GetItem() => currentItem;
    public int GetCount() => currentCount;
}