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

        // Сховати системний курсор на старті
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
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

        // Ставимо гру на паузу
        Time.timeScale = isInventoryOpen ? 0f : 1f;
    }

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }
}