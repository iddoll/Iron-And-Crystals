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

        if (draggedIcon) draggedIcon.gameObject.SetActive(false);
        if (draggedIconText) draggedIconText.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (HasItem())
        {
            UpdateDraggedPosition(Input.mousePosition);
        }
    }

    public void StartDragging(InventorySlot originSlot, Item item, int amount, Sprite iconSprite)
    {
        sourceSlot = originSlot;
        draggedItem = item;
        draggedCount = amount;

        if (draggedIcon != null)
        {
            draggedIcon.sprite = iconSprite;
            draggedIcon.gameObject.SetActive(true);
        }

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

        // одразу ставимо іконку під курсор
        UpdateDraggedPosition(Input.mousePosition);
    }



    public void UpdateDraggedPosition(Vector2 position)
    {
        if (draggedIcon) draggedIcon.transform.position = position;
        if (draggedIconText) draggedIconText.transform.position = position + new Vector2(20, -20);
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
                InventorySlot startSlot = GetSourceSlot();
                Item itemToDrop = GetItem();
                int amount = GetCount();

                if (startSlot != null && itemToDrop != null && amount > 0)
                {
                    // Викидаємо стільки штук, скільки тягнули
                    PlayerController.Instance.DropItemFromInventory(itemToDrop, amount);
                    // НЕ чистимо слот тут — InventorySystem.RemoveItems усередині DropItemFromInventory зробить це
                }
            }
        }

        StopDragging();
    }

    public void StopDragging()
    {
        draggedItem = null;
        draggedCount = 0;
        sourceSlot = null;
        if (draggedIcon) draggedIcon.gameObject.SetActive(false);
        if (draggedIconText) draggedIconText.gameObject.SetActive(false);
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
