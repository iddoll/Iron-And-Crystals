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
    private EquipmentSlot sourceEquipSlot;

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
    
    // Перевантажений метод для InventorySlot
    public void StartDragging(InventorySlot originSlot, Item item, int amount, Sprite iconSprite)
    {
        sourceSlot = originSlot;
        sourceEquipSlot = null;
        draggedItem = item;
        draggedCount = amount;
        UpdateUI(iconSprite);
    }
    
    // Перевантажений метод для EquipmentSlot
    public void StartDragging(EquipmentSlot originEquipSlot, Item item, int amount, Sprite iconSprite)
    {
        sourceEquipSlot = originEquipSlot;
        sourceSlot = null;
        draggedItem = item;
        draggedCount = amount;
        UpdateUI(iconSprite);
    }
    
    private void UpdateUI(Sprite iconSprite)
    {
        if (draggedIcon != null)
        {
            draggedIcon.sprite = iconSprite;
            draggedIcon.gameObject.SetActive(true);
        }
        draggedIconText.text = draggedCount > 1 ? draggedCount.ToString() : "";
        draggedIconText.gameObject.SetActive(draggedCount > 1);
        UpdateDraggedPosition(Input.mousePosition);
    }

    public void UpdateDraggedPosition(Vector2 position)
    {
        if (draggedIcon) draggedIcon.transform.position = position;
        if (draggedIconText) draggedIconText.transform.position = position + new Vector2(20, -20);
    }

    public void OnEndDrag(PointerEventData eventData, MonoBehaviour sourceObject)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        bool droppedOnValidTarget = false;
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<InventorySlot>() != null || result.gameObject.GetComponent<EquipmentSlot>() != null)
            {
                droppedOnValidTarget = true;
                break;
            }
        }
        
        // Якщо предмет був кинутий поза UI
        if (!droppedOnValidTarget)
        {
            if (HasItem())
            {
                Item itemToDrop = GetItem();
                int amount = GetCount();
        
                if (sourceSlot != null) // Якщо тягнули зі звичайного інвентарю
                {
                    // 1. Створюємо об'єкт у світі
                    PlayerController.Instance.DropItemFromInventory(itemToDrop, amount);
            
                    // 2. ОЧИЩАЄМО слот-джерело (щоб предмет не дублювався)
                    sourceSlot.ClearSlot(); 
                }
                else if (sourceEquipSlot != null) // Якщо тягнули з комірки шолома/щита
                {
                    // Тут ми просто повертаємо його назад у слот, якщо кинули "в нікуди"
                    sourceEquipSlot.SetItem(itemToDrop);
                }
            }
        }
        
        // Завжди зупиняємо перетягування після обробки
        StopDragging();
    }

    public void StopDragging()
    {
        draggedItem = null;
        draggedCount = 0;
        sourceSlot = null;
        sourceEquipSlot = null;
        if (draggedIcon) draggedIcon.gameObject.SetActive(false);
        if (draggedIconText) draggedIconText.gameObject.SetActive(false);
    }

    public bool HasItem() => draggedItem != null;
    public Item GetItem() => draggedItem;
    public int GetCount() => draggedCount;
    public InventorySlot GetSourceSlot() => sourceSlot;
    public EquipmentSlot GetSourceEquipSlot() => sourceEquipSlot;
    public bool IsDraggingItem(Item item) => draggedItem == item;
}