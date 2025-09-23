using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquipmentSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemType allowedType = ItemType.Helmet;
    public Image icon;

    private Item currentItem;

    // --- Логіка перетягування (Drag and Drop) ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;
        
        // Передаємо в InventoryDragManager інформацію про предмет
        InventoryDragManager.Instance.StartDragging(this, currentItem, 1, icon.sprite);
        
        // Очищаємо слот, щоб запобігти дублюванню
        if (PlayerEquipment.Instance != null)
            PlayerEquipment.Instance.UnequipItem(currentItem.itemType);
        currentItem = null;
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        InventoryDragManager.Instance.UpdateDraggedPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Перевіряємо, чи було перетягування успішним (чи OnDrop спрацював)
        GameObject target = eventData.pointerEnter;
        bool droppedOnSlot = target != null && (target.GetComponent<InventorySlot>() != null || target.GetComponent<EquipmentSlot>() != null);
        
        // Якщо перетягування невдале, повертаємо предмет в його початковий слот.
        // Це робиться за допомогою InventoryDragManager, який знає, звідки предмет походить.
        InventoryDragManager.Instance.OnEndDrag(eventData, this);
    }

    // --- Логіка OnDrop ---

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryDragManager.Instance.HasItem()) return;

        Item droppedItem = InventoryDragManager.Instance.GetItem();
        InventorySlot sourceSlot = InventoryDragManager.Instance.GetSourceSlot();
        EquipmentSlot sourceEquipmentSlot = InventoryDragManager.Instance.GetSourceEquipSlot();

        if (droppedItem == null || droppedItem.itemType != allowedType) 
        {
            return;
        }

        if (sourceEquipmentSlot == this) return;
        
        Item tempItem = currentItem;
        
        SetItem(droppedItem);
        
        if (sourceSlot != null)
        {
            if (tempItem != null)
            {
                sourceSlot.AddItem(tempItem, 1);
            }
            else
            {
                sourceSlot.ClearSlot();
            }
        }
        else if (sourceEquipmentSlot != null)
        {
            if (tempItem != null)
            {
                sourceEquipmentSlot.SetItem(tempItem);
            }
            else
            {
                sourceEquipmentSlot.ClearSlot();
            }
        }
        
        InventoryDragManager.Instance.StopDragging();
    }

    // --- Внутрішні методи ---

    public void SetItem(Item newItem)
    {
        currentItem = newItem;
        if (icon != null)
        {
            icon.sprite = newItem.icon;
            icon.enabled = true;
        }
        if (PlayerEquipment.Instance != null)
            PlayerEquipment.Instance.EquipItem(newItem);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        if (currentItem != null)
        {
            bool added = InventorySystem.Instance.AddItem(currentItem);
            if (added)
            {
                if (PlayerEquipment.Instance != null)
                    PlayerEquipment.Instance.UnequipItem(currentItem.itemType);
                currentItem = null;
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }
            else
            {
                if (currentItem.worldPrefab != null)
                    Instantiate(currentItem.worldPrefab, PlayerController.Instance.transform.position + Vector3.up * 0.5f, Quaternion.identity);
                else
                    Debug.LogWarning($"Could not add {currentItem.itemName} to inventory and there is no worldPrefab.");
                if (PlayerEquipment.Instance != null)
                    PlayerEquipment.Instance.UnequipItem(currentItem.itemType);
                currentItem = null;
                if (icon != null)
                {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }
        }
    }
}