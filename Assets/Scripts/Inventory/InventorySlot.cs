using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler // Додаємо IPointerClickHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image icon;      // Іконка предмета (не фон!)
    [SerializeField] private Text countText;  // Текст кількості

    private Item currentItem;
    private int count;
    public int slotIndex; // Додати у InventorySlot

    // Прапорець для відстеження, чи був запущений Drag через Ctrl+Click
    private bool isSplittingDrag = false; 

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Якщо ми вже почали розстакування через OnPointerClick, ігноруємо стандартний Drag
        if (isSplittingDrag) 
        {
            isSplittingDrag = false; // Скидаємо прапорець
            return; 
        }

        if (IsEmpty()) return;
        InventoryDragManager.Instance.StartDragging(this, currentItem, count, icon.sprite);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Ми завжди оновлюємо позицію, незалежно від того, як почався Drag
        InventoryDragManager.Instance.UpdateDraggedPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Важливо: тепер ми не обробляємо логіку тут
        // Просто делегуємо її InventoryDragManager
        InventoryDragManager.Instance.OnEndDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryDragManager.Instance.HasItem()) return;

        InventorySlot fromSlot = InventoryDragManager.Instance.GetSourceSlot();
        if (fromSlot == this) return;

        Item incomingItem = InventoryDragManager.Instance.GetItem();
        int incomingCount = InventoryDragManager.Instance.GetCount();

        // Якщо ми перетягуємо предмет, який зараз активний у InventorySystem
        if (InventorySystem.Instance.GetActiveSlotIndex() == fromSlot.slotIndex)
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

                fromSlot.ReduceStack(amountToStack);

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
        fromSlot.AddItem(tempItem, tempCount);

        // Якщо новий слот став активним, можна екіпірувати
        if (InventorySystem.Instance.GetActiveSlotIndex() == slotIndex)
        {
            Item item = GetItem();
            if (item != null && item.equippedPrefab != null)
                PlayerController.Instance.EquipItem(item);
        }
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

        // Якщо цей слот активний, одразу екіпірувати предмет
        if (InventorySystem.Instance.GetActiveSlotIndex() == slotIndex)
            InventorySystem.Instance.UpdateActiveItem();
    }

    public void ClearSlot()
    {
        currentItem = null;
        count = 0;
        RefreshUI();

        QuiqSlot.Instance?.OnSlotChanged(this);

        // Якщо цей слот активний, оновлюємо екіпірування
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
        else
        {
            Debug.LogWarning("Спроба додати предмет до заповненого стаку або неіснуючого предмета.");
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
        else
        {
            Debug.LogWarning("Спроба видалити предмет з порожнього або неіснуючого стаку.");
        }
    }
    
    // Змінено OnPointerClick, щоб обробляти Ctrl+Click для розстакування
    public void OnPointerClick(PointerEventData eventData)
{
    if (eventData.button != PointerEventData.InputButton.Left) return;

    // --- Ctrl+Click: забрати по одному ---
    if (Input.GetKey(KeyCode.LeftControl) && !IsEmpty() && currentItem.isStackable)
    {
        if (!InventoryDragManager.Instance.HasItem())
        {
            // Беремо 1 з цього слота
            InventoryDragManager.Instance.StartDragging(this, currentItem, 1, icon.sprite);
            RemoveOne();
        }
        else if (InventoryDragManager.Instance.IsDraggingItem(currentItem) && GetCurrentCount() > 0)
        {
            // Додаємо ще 1 в курсор
            InventoryDragManager.Instance.IncreaseDraggedCount(1);
            RemoveOne();
        }
        return;
    }

    // --- Якщо вже є щось у курсорі: намагаємось покласти ---
    if (InventoryDragManager.Instance.HasItem())
    {
        Item draggedItem = InventoryDragManager.Instance.GetItem();
        int draggedCount = InventoryDragManager.Instance.GetCount();

        // Порожній слот → кладемо все
        if (IsEmpty())
        {
            AddItem(draggedItem, draggedCount);
            InventoryDragManager.Instance.StopDragging();
            return;
        }

        // Такий самий предмет → додаємо в стак
        if (currentItem == draggedItem && currentItem.isStackable)
        {
            int spaceLeft = currentItem.maxStack - count;
            if (spaceLeft > 0)
            {
                int toAdd = Mathf.Min(spaceLeft, draggedCount);
                count += toAdd;
                RefreshUI();

                if (draggedCount > toAdd)
                {
                    InventoryDragManager.Instance.StartDragging(
                        InventoryDragManager.Instance.GetSourceSlot(),
                        draggedItem,
                        draggedCount - toAdd,
                        draggedItem.icon
                    );
                }
                else
                {
                    InventoryDragManager.Instance.StopDragging();
                }
            }
            return;
        }

        // Якщо інший предмет → нічого не робимо
        return;
    }

    // --- Якщо курсор пустий і немає Ctrl ---
    if (!IsEmpty())
    {
        // Взяти весь стак у курсор
        InventoryDragManager.Instance.StartDragging(this, currentItem, count, icon.sprite);
        ClearSlot();
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
        else // Слот порожній
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