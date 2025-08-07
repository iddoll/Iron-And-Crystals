using UnityEngine;
using UnityEngine.UI;

public class InventoryDragManager : MonoBehaviour
{
    public static InventoryDragManager Instance;

    [SerializeField] private Canvas canvas;
    [SerializeField] private Image draggedIcon;
    [SerializeField] private Text draggedIconText; // 🔧 Додано

    private Item draggedItem;
    private int draggedCount;
    private InventorySlot sourceSlot;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        draggedIcon.gameObject.SetActive(false);
        draggedIconText.gameObject.SetActive(false); // 🔧 Ховаємо на початку
    }

    public void StartDragging(InventorySlot slot, Item item, int count, Sprite iconSprite)
    {
        draggedItem = item;
        draggedCount = count;
        sourceSlot = slot;

        draggedIcon.sprite = iconSprite;
        draggedIcon.enabled = true;
        draggedIcon.gameObject.SetActive(true);

        UpdateDraggedText(); // 🔧 Оновлюємо текст
    }

    public void UpdateDraggedPosition(Vector2 position)
    {
        draggedIcon.transform.position = position;
        draggedIconText.transform.position = position + new Vector2(20, -20); // 🔧 Зсув для тексту
    }

    public void StopDragging()
    {
        draggedItem = null;
        sourceSlot = null;
        draggedIcon.gameObject.SetActive(false);
        draggedIconText.gameObject.SetActive(false); // 🔧 Ховаємо
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
