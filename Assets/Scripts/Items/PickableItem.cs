using UnityEngine;

public class PickableItem : MonoBehaviour
{
    public Item itemToPickup;
    public bool isHeld = false; // Важливо, щоб цей прапорець використовувався коректно

    public float pickupRadius = 1f;

    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (itemToPickup == null)
        {
            Debug.LogError($"PickableItem на {gameObject.name} не має призначеного ItemToPickup! Будь ласка, призначте його в Inspector.");
            return;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length == 0)
        {
            Debug.LogError($"PickableItem на {gameObject.name} не має жодного Collider2D! Підбір не працюватиме.");
        }
        else if (itemToPickup.pickupMethod == PickupMethod.OnTouch)
        {
            bool hasTrigger = false;
            foreach (var col in colliders)
            {
                if (col.isTrigger) { hasTrigger = true; break; }
            }
            if (!hasTrigger)
            {
                Debug.LogWarning($"PickableItem {gameObject.name} з PickupMethod.OnTouch не має жодного тригер-колайдера!");
            }
        }
    }

    private void Update()
    {
        // Debug.Log($"[PickableItem] Update for {gameObject.name}. isHeld: {isHeld}"); // Може бути дуже багато логів
        if (isHeld || player == null || itemToPickup == null) return;

        if (itemToPickup.pickupMethod == PickupMethod.OnEPress)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= pickupRadius && Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[PickableItem] Input.GetKeyDown(E) поруч з предметом OnEPress: {gameObject.name}");
                TryPickup();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[PickableItem] OnTriggerEnter2D викликано на {gameObject.name} з {collision.gameObject.name}");
        if (isHeld || itemToPickup == null) return;

        if (itemToPickup.pickupMethod == PickupMethod.OnTouch)
        {
            if (collision.CompareTag("Player"))
            {
                Debug.Log($"[PickableItem] Гравець торкнувся предмета OnTouch: {gameObject.name}");
                TryPickup();
            }
        }
    }

    private void TryPickup()
    {
        Debug.Log($"[PickableItem] TryPickup() викликано для: {gameObject.name} | isHeld на початку: {isHeld}");
        if (isHeld)
        {
            Debug.Log($"[PickableItem] Предмет {gameObject.name} вже позначений як Held, відміна підбору.");
            return;
        }
        
        isHeld = true; // Встановлюємо isHeld в true ДО виклику AddItem
        Debug.Log($"[PickableItem] isHeld встановлено в true для {gameObject.name}.");

        bool success = InventorySystem.Instance.AddItem(itemToPickup);
        Debug.Log($"[PickableItem] Результат InventorySystem.Instance.AddItem для {itemToPickup.itemName}: {success}");

        if (success)
        {
            Debug.Log($"[PickableItem] {itemToPickup.itemName} успішно додано в інвентар. Готуюсь знищити об'єкт: {gameObject.name}");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"[PickableItem] Не вдалося додати {itemToPickup.itemName} в інвентар. Можливо, інвентар повний? Скидаю isHeld на false для {gameObject.name}.");
            isHeld = false; // Якщо додати не вдалося, дозволяємо спробувати підібрати знову
        }
    }
}