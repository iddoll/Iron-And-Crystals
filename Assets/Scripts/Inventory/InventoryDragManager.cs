using System.Collections;
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
        
            // ВАЖЛИВО: Переконайся, що іконка не прозора
            Color c = draggedIcon.color;
            c.a = 1f; 
            draggedIcon.color = c;

            // Виносимо іконку на передній план UI
            draggedIcon.transform.SetAsLastSibling();
        }
    
        // Показуємо кількість, якщо > 1 або якщо це стріли
        bool shouldShowText = draggedCount > 1 || (draggedItem != null && draggedItem.itemType == ItemType.Arrow);
    
        if (draggedIconText != null)
        {
            draggedIconText.text = draggedCount.ToString();
            draggedIconText.gameObject.SetActive(shouldShowText);
            draggedIconText.transform.SetAsLastSibling();
        }
    
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
            // Перевіряємо, чи є під мишкою будь-який об'єкт, що може прийняти предмет
            if (result.gameObject.GetComponent<InventorySlot>() != null || 
                result.gameObject.GetComponent<EquipmentSlot>() != null)
            {
                droppedOnValidTarget = true;
                break;
            }
        }
    
        if (!droppedOnValidTarget)
        {
            // ЛОГІКА ВИКИДАННЯ У СВІТ
            if (HasItem())
            {
                PlayerController.Instance.DropItemFromInventory(GetItem(), GetCount());
                // sourceSlot вже був очищений у OnBeginDrag, тому нічого не робимо
            }
        }
        else
        {
            // ЛОГІКА ПЕРЕВІРКИ: ЧИ ПРИЙНЯЛИ ПРЕДМЕТ?
            // Якщо OnDrop спрацював успішно, він викликає StopDragging() всередині себе.
            // Якщо ж OnDrop відхилив предмет через тип, StopDragging() ще не був викликаний.
        
            StartCoroutine(ReturnItemIfNotDropped());
        }
    }

// Корутина, яка чекає один кадр, щоб OnDrop встиг спрацювати
    private IEnumerator ReturnItemIfNotDropped()
    {
        yield return new WaitForEndOfFrame();

        if (HasItem()) // Якщо після завершення кадру предмет все ще в менеджері
        {
            if (sourceSlot != null)
            {
                // Повертаємо в початковий слот (тут все добре)
                sourceSlot.AddItem(draggedItem, draggedCount);
            }
            else if (sourceEquipSlot != null)
            {
                // ВИПРАВЛЕНО: Додаємо другий аргумент (кількість), як того вимагає новий EquipmentSlot
                sourceEquipSlot.SetItem(draggedItem, draggedCount);
            }
    
            StopDragging();
        }
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