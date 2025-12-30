using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image icon;
    [SerializeField] private Text countText;

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

        InventorySlot fromSlot = InventoryDragManager.Instance.GetSourceSlot();
        EquipmentSlot fromEquipSlot = InventoryDragManager.Instance.GetSourceEquipSlot();

        Item incomingItem = InventoryDragManager.Instance.GetItem();
        int incomingCount = InventoryDragManager.Instance.GetCount();

        // 1. Спроба стакування з предметом, який вже є в слоті
        if (!IsEmpty() && CanStack(incomingItem))
        {
            int spaceLeft = currentItem.maxStack - count;
            int amountToStack = Mathf.Min(spaceLeft, incomingCount);
            
            count += amountToStack;
            RefreshUI();

            // Якщо залишився залишок після стакування - продовжуємо його тягнути
            if (incomingCount > amountToStack)
                InventoryDragManager.Instance.StartDragging(fromSlot, incomingItem, incomingCount - amountToStack, incomingItem.icon);
            else
                InventoryDragManager.Instance.StopDragging();

            return;
        }

        // 2. Обмін предметами (Swap)
        Item tempItem = currentItem;
        int tempCount = count;

        AddItem(incomingItem, incomingCount);

        // Повертаємо старий предмет туди, звідки прийшов новий
        if (fromSlot != null && fromSlot != this)
        {
            fromSlot.AddItem(tempItem, tempCount);
        }
        else if (fromEquipSlot != null)
        {
            if (tempItem != null && tempItem.itemType == fromEquipSlot.allowedType)
                fromEquipSlot.SetItem(tempItem);
            else
                InventorySystem.Instance.AddItem(tempItem); // Якщо не підходить в екіпіровку, кидаємо в загальний інвентар
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
            
            // ПУНКТ 4 та 6: цифра тільки для стаків > 1
            if (currentItem.isStackable && count > 1)
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

    public bool IsEmpty() => currentItem == null;
    public bool CanStack(Item item) => !IsEmpty() && currentItem == item && currentItem.isStackable && count < currentItem.maxStack;
    public Item GetItem() => currentItem;
    public int GetCurrentCount() => count;
    public void ReduceStack(int amount) { count -= amount; if (count <= 0) ClearSlot(); else RefreshUI(); }
}