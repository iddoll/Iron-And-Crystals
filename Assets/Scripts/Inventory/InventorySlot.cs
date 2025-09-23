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

    private bool isSplittingDrag = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isSplittingDrag)
        {
            isSplittingDrag = false;
            return;
        }

        if (IsEmpty()) return;
        InventoryDragManager.Instance.StartDragging(this, currentItem, count, icon.sprite);
        // Не очищаємо слот тут, щоб уникнути зникнення предмета при невдалому перетягуванні
    }

    public void OnDrag(PointerEventData eventData)
    {
        InventoryDragManager.Instance.UpdateDraggedPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryDragManager.Instance.OnEndDrag(eventData, this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryDragManager.Instance.HasItem()) return;

        // Отримуємо джерело перетягування
        InventorySlot fromSlot = InventoryDragManager.Instance.GetSourceSlot();
        EquipmentSlot fromEquipSlot = eventData.pointerDrag?.GetComponent<EquipmentSlot>();

        if (fromSlot == this || fromEquipSlot == this) return;

        Item incomingItem = InventoryDragManager.Instance.GetItem();
        int incomingCount = InventoryDragManager.Instance.GetCount();

        if (fromSlot != null && InventorySystem.Instance.GetActiveSlotIndex() == fromSlot.slotIndex)
        {
            PlayerController.Instance.UnequipItem();
            InventorySystem.Instance.SetCurrentTool("None");
        }

        // Логіка стекування
        if (!IsEmpty() && CanStack(incomingItem))
        {
            int spaceLeft = currentItem.maxStack - count;
            if (spaceLeft > 0)
            {
                int amountToStack = Mathf.Min(spaceLeft, incomingCount);
                count += amountToStack;
                RefreshUI();

                if (fromSlot != null)
                {
                    fromSlot.ReduceStack(amountToStack);
                }
                else if (fromEquipSlot != null)
                {
                    fromEquipSlot.ClearSlot();
                }

                if (incomingCount > amountToStack)
                    InventoryDragManager.Instance.StartDragging(fromSlot, incomingItem, incomingCount - amountToStack, incomingItem.icon);
                else
                    InventoryDragManager.Instance.StopDragging();

                return;
            }
        }

        // Обмін предметами
        Item tempItem = currentItem;
        int tempCount = count;

        AddItem(incomingItem, incomingCount);

        if (fromSlot != null)
        {
            fromSlot.AddItem(tempItem, tempCount);
        }
        else if (fromEquipSlot != null)
        {
            if (tempItem != null)
            {
                // Якщо в InventorySlot був предмет, повертаємо його в EquipmentSlot
                fromEquipSlot.SetItem(tempItem);
            }
            else
            {
                fromEquipSlot.ClearSlot();
            }
        }

        if (InventorySystem.Instance.GetActiveSlotIndex() == slotIndex)
        {
            Item item = GetItem();
            if (item != null && item.equippedPrefab != null)
                PlayerController.Instance.EquipItem(item);
        }
        
        InventoryDragManager.Instance.StopDragging();
    }

    public void ReduceStack(int amount)
    {
        count -= amount;
        if (count <= 0)
        {
            ClearSlot();
        }
        else
        {
            RefreshUI();
        }
    }

    public void AddItem(Item item, int amount)
    {
        currentItem = item;
        count = amount;
        RefreshUI();

        QuiqSlot.Instance?.OnSlotChanged(this);

        if (InventorySystem.Instance.GetActiveSlotIndex() == slotIndex)
            InventorySystem.Instance.UpdateActiveItem();
    }

    public void ClearSlot()
    {
        currentItem = null;
        count = 0;
        RefreshUI();

        QuiqSlot.Instance?.OnSlotChanged(this);

        if (InventorySystem.Instance.GetActiveSlotIndex() == slotIndex)
            InventorySystem.Instance.UpdateActiveItem();
    }
    public bool IsEmpty() => currentItem == null;

    public void AddItem(Item item)
    {
        currentItem = item;
        count = 1;
        RefreshUI();
        if (QuiqSlot.Instance != null)
        {
            QuiqSlot.Instance.OnSlotChanged(this);
        }
    }

    public void AddOne()
    {
        if (currentItem != null && count < currentItem.maxStack)
        {
            count++;
            RefreshUI();
        }
    }

    public void RemoveOne()
    {
        if (currentItem != null && count > 0)
        {
            count--;
            if (count == 0)
            {
                ClearSlot();
            }
            else
            {
                RefreshUI();
            }
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryDragManager.Instance.HasItem() && eventData.button == PointerEventData.InputButton.Left)
        {
            OnDrop(eventData);
            return;
        }

        if (!InventoryDragManager.Instance.HasItem())
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (!IsEmpty())
                {
                    PlayerController.Instance.DropItemFromInventory(currentItem, 1);
                    ReduceStack(1);
                }
            }
            else if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (!IsEmpty())
                {
                    InventoryDragManager.Instance.StartDragging(this, currentItem, count, icon.sprite);
                    ClearSlot();
                }
            }
        }
    }

    private void RefreshUI()
    {
        if (icon == null) 
        {
            Debug.LogError($"[RefreshUI ERROR] icon (Image component) is NULL for slot: {gameObject.name}! Assign it in the Inspector.");
            return;
        }
        if (countText == null) 
        {
            Debug.LogError($"[RefreshUI ERROR] countText (Text component) is NULL for slot: {gameObject.name}! Assign it in the Inspector.");
            return;
        }

        if (currentItem != null)
        {
            if (currentItem.icon == null) 
            {
                Debug.LogError($"[RefreshUI ERROR] currentItem.icon is NULL for item: {currentItem.itemName}! Please assign an icon in the ScriptableObject.");
                icon.sprite = null;
                icon.enabled = false;
                countText.text = "";
                countText.enabled = false;
                return;
            }

            icon.sprite = currentItem.icon;
            icon.enabled = true;
        
            countText.text = currentItem.isStackable && count > 1 ? count.ToString() : "";
            countText.enabled = currentItem.isStackable && count > 1;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
            countText.text = "";
            countText.enabled = false;
        }
    }

    public bool CanStack(Item item) =>
        !IsEmpty() && currentItem == item && currentItem.isStackable && count < currentItem.maxStack;
    
    public Item GetItem() => currentItem;
    public int GetCurrentCount() => count;
}