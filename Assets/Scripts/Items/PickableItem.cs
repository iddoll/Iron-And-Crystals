using UnityEngine;

public class PickableItem : MonoBehaviour
{
    public Item itemToPickup;
    public int amount = 1; // Скільки штук підберемо (наприклад, 10 стріл)
    public float pickupRadius = 2f; // Радіус підбору

    private bool isHeld = false;
    private Transform player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Перевірки в Start, щоб ти бачив помилки в консолі
        if (itemToPickup == null)
            Debug.LogError($"[PickableItem] {gameObject.name} не має призначеного Item!");
    }

    private void Update()
    {
        if (isHeld || player == null || itemToPickup == null) return;

        // 1. Логіка для підбору на клавішу E
        if (itemToPickup.pickupMethod == PickupMethod.OnEPress)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            
            // Якщо гравець поруч і натиснув E
            if (distance <= pickupRadius && Input.GetKeyDown(KeyCode.E))
            {
                TryPickup();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isHeld || itemToPickup == null) return;

        // 2. Логіка для підбору просто торканням
        if (itemToPickup.pickupMethod == PickupMethod.OnTouch)
        {
            if (collision.CompareTag("Player"))
            {
                TryPickup();
            }
        }
    }

    private void TryPickup()
    {
        if (isHeld) return;
        
        isHeld = true; 

        // Передаємо предмет в інвентар
        // Якщо твій AddItem приймає лише Item, то він додасть 1 шт.
        // Якщо ми хочемо стаки, треба буде трохи змінити AddItem пізніше.
        bool success = InventorySystem.Instance.AddItem(itemToPickup);

        if (success)
        {
            Debug.Log($"[PickableItem] Підібрано: {itemToPickup.itemName}");
            Destroy(gameObject);
        }
        else
        {
            isHeld = false; // Інвентар повний
        }
    }

    // Візуалізація радіусу в редакторі, щоб ти бачив зону підбору
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}