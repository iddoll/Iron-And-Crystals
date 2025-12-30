using UnityEngine;

public class ShieldController : MonoBehaviour
{
    public static ShieldController Instance;

    [Header("Block settings")]
    public bool isEquipped = false;        // чи є щит в EquipmentSlot
    public bool IsBlocking { get; private set; } = false;

    [Tooltip("Множник швидкості під час блоку (наприклад 0.3 = 30% швидкості)")]
    public float blockMoveMultiplier = 0.3f;

    [Tooltip("Пасивний множник коли щит просто в руці (не блокує)")]
    public float passiveDamageMultiplier = 0.8f; // -20%

    // Активний блок: для melee/explosion множимо на activeDamageMultiplier, для projectile — 0
    [Tooltip("Множник до урону при активному блоці (melee/explosion)")]
    public float activeDamageMultiplier = 0.3f; // залишає 30% від урону

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        // Якщо щит не екіпований — не реагуємо на вхід
        if (!isEquipped) 
        {
            if (IsBlocking)
            {
                // якщо раніше ломали (теоретично) — припиняємо
                StopBlocking();
            }
            return;
        }

        // Блокування — утримування ПКМ
        bool wantBlock = Input.GetMouseButton(1); // ПКМ утримуємо
        if (wantBlock && !IsBlocking)
        {
            StartBlocking();
        }
        else if (!wantBlock && IsBlocking)
        {
            StopBlocking();
        }
    }

    private void StartBlocking()
    {
        IsBlocking = true;
        // Повідомляємо PlayerController: перервати атаку, вимкнути можливість атакувати
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnStartBlocking();
    }

    private void StopBlocking()
    {
        IsBlocking = false;
        if (PlayerController.Instance != null)
            PlayerController.Instance.OnStopBlocking();
    }

    // Викликається з PlayerEquipment коли екіпірують/знімають щит
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;

        // якщо зняли щит — автоматично припиняємо блокувати
        if (!isEquipped && IsBlocking)
            StopBlocking();
    }

    // Головна функція — дає фінальний урон після врахування щита
    // Просто DamageType (без PlayerController перед ним)
    public float ModifyDamage(float amount, DamageType type)
    {
        if (!isEquipped) return amount;
        if (!IsBlocking) return amount * passiveDamageMultiplier;

        switch (type)
        {
            case DamageType.Projectile:
                return 0f;
            case DamageType.Melee:
            case DamageType.Explosion:
            default:
                return amount * activeDamageMultiplier;
        }
    }
}
