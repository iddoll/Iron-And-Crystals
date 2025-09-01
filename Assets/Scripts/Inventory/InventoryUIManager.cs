using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance;

    [SerializeField] private GameObject inventoryUI; // панель з повним інвентарем

    private bool isInventoryOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryUI.SetActive(isInventoryOpen);

        Time.timeScale = isInventoryOpen ? 0f : 1f;

        // Показати / сховати курсор
        Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isInventoryOpen;
    }

    // 👇 Оце додаємо
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }
}