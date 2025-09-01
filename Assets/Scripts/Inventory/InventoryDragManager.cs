using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class InventoryDragManager : MonoBehaviour
{
    public static InventoryDragManager Instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Image draggedIcon;
    [SerializeField] private Text draggedIconText;

    private Item draggedItem;
    private int draggedCount;
    private InventorySlot sourceSlot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        draggedIcon.gameObject.SetActive(false);
        draggedIconText.gameObject.SetActive(false);
    }

    public void StartDragging(InventorySlot slot, Item item, int count, Sprite iconSprite)
    {
        draggedItem = item;
        draggedCount = count;
        sourceSlot = slot;

        draggedIcon.sprite = iconSprite;
        draggedIcon.enabled = true;
        draggedIcon.gameObject.SetActive(true);

        UpdateDraggedText();
    }

    public void UpdateDraggedPosition(Vector2 position)
    {
        draggedIcon.transform.position = position;
        draggedIconText.transform.position = position + new Vector2(20, -20);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        bool isOverUI = results.Count > 0;

        if (!isOverUI)
        {
            if (HasItem())
            {
                // 🔧 Отримуємо початковий слот, з якого почалося перетягування.
                InventorySlot startSlot = GetSourceSlot();
                Item itemToDrop = GetItem();
            
                if (startSlot != null && itemToDrop != null)
                {
                    // 🔧 Викликаємо метод викидання предмета.
                    PlayerController.Instance.DropItemFromInventory(itemToDrop);
                    
                    // 🔧 Очищаємо початковий слот, щоб уникнути дублювання.
                    startSlot.ClearSlot();
                }
            }
        }
        
        StopDragging();
    }

    public void StopDragging()
    {
        draggedItem = null;
        sourceSlot = null;
        draggedIcon.gameObject.SetActive(false);
        draggedIconText.gameObject.SetActive(false);
    }

    public bool HasItem() => draggedItem != null;

    public Item GetItem() => draggedItem;
    public int GetCount() => draggedCount;
    public InventorySlot GetSourceSlot() => sourceSlot;
    public bool IsDraggingItem(Item item) => draggedItem == item;

    public void IncreaseDraggedCount(int amount)
    {
        if (draggedItem == null) return;

        draggedCount += amount;
        UpdateDraggedText();
    }

    private void UpdateDraggedText()
    {
        if (draggedCount > 1)
        {
            draggedIconText.text = draggedCount.ToString();
            draggedIconText.gameObject.SetActive(true);
        }
        else
        {
            draggedIconText.text = "";
            draggedIconText.gameObject.SetActive(false);
        }
    }
}
