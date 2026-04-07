using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("Unarmed Attacks")]
    public float unarmedDamage = 10f;

    [Header("Attack Zones")]
    public AttackZone swordAttackZone;
    public AttackZone axeAttackZone;
    public AttackZone lanceAttackZone;
    public AttackZone punchAttackZone;

    [Header("Bow Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float bowCooldown = 0.5f;
    public float maxChargeTime = 1.5f; 
    public float minArrowSpeed = 10f;  
    public float maxArrowSpeed = 30f;  

    private float chargeTimer = 0f;
    private bool isCharging = false;
    private float lastShotTime;

    private Animator animator;
    private PlayerController player;

    public bool IsAttacking { get; private set; }
    public bool CanAttack { get; private set; } = true;
    public bool IsShooting { get; private set; }

    void Awake()
    {
        animator = GetComponent<Animator>();
        player = GetComponent<PlayerController>();
    }

    // Цей метод тепер викликає ТІЛЬКИ ближній бій
    public void HandleAttack(Item currentItem)
    {
        if (currentItem == null)
        {
            DoUnarmedAttack();
            return;
        }

        switch (currentItem.itemType)
        {
            case ItemType.Lance:
                StartCoroutine(SpearAttackRoutine(currentItem.attackCooldown));
                break;
            case ItemType.Sword:
            case ItemType.Axe:
                IsAttacking = true;
                CanAttack = false;
                animator.SetBool("isAttacking", true);
                StartCoroutine(AttackCooldownCoroutine(currentItem.attackCooldown));
                break;
            case ItemType.Bow:
                // Для лука ми нічого не робимо тут, бо він працює через HandleBowCharging в Update
                break;
            default:
                DoUnarmedAttack();
                break;
        }
    }

    // НОВА ЛОГІКА ДЛЯ ЛУКА (Викликається з PlayerController Update)
    public void HandleBowCharging()
    {
        if (Input.GetMouseButtonDown(0) && CanAttack && Time.time >= lastShotTime + bowCooldown)
        {
            if (InventorySystem.Instance.HasItemOfType(ItemType.Arrow))
            {
                isCharging = true;
                chargeTimer = 0f;
                IsShooting = true;
                animator.SetBool("isShooting", true); 
                animator.SetFloat("bowChargeProgress", 0f);
            }
        }

        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(chargeTimer / maxChargeTime);
            animator.SetFloat("bowChargeProgress", progress);

            if (Input.GetMouseButtonUp(0))
            {
                FireArrow(progress);
                isCharging = false;
                IsShooting = false;
                animator.SetBool("isShooting", false);
                lastShotTime = Time.time;
            }
        }
    }

    // Додай цей метод в Update або викликай його, коли змінюється інвентар
    public void UpdateBowAmmoDisplay()
    {
        if (player.currentEquippedItem == null || player.currentEquippedItem.itemType != ItemType.Bow) return;

        int totalAmmo;
        // Отримуємо предмет стріли та загальну кількість через InventorySystem
        Item arrowToUse = InventorySystem.Instance.GetAmmoToUse(out totalAmmo);

        // Шукаємо текст на префабі лука, який зараз у руках (heldObject)
        if (player.heldObject != null)
        {
            var ammoText = player.heldObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (ammoText != null)
            {
                ammoText.text = totalAmmo.ToString();
                // Опціонально: можна змінювати колір тексту, якщо стріл 0
                ammoText.color = totalAmmo > 0 ? Color.white : Color.red;
            }
        }
    }

    private void FireArrow(float chargeProgress)
    {
        if (arrowPrefab != null && firePoint != null)
        {
            int dummyCount;
            // 1. Питаємо інвентар, яку стрілу використовувати (пріоритет слота)
            Item arrowItem = InventorySystem.Instance.GetAmmoToUse(out dummyCount);

            if (arrowItem != null)
            {
                // 2. Видаляємо саме ту стрілу, яку знайшли (спочатку зі слота, потім з інвентарю)
                InventorySystem.Instance.ConsumeArrow();

                float finalSpeed = Mathf.Lerp(minArrowSpeed, maxArrowSpeed, chargeProgress);
                GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
                Arrow arrow = arrowObj.GetComponent<Arrow>();
            
                // Якщо у стріли є свої статси (наприклад, бонус до дамагу), передаємо їх тут
                arrow.speed = finalSpeed; 
                arrow.Shoot(player.transform.localScale.x > 0);
            
                // Оновлюємо цифри на лукові після пострілу
                UpdateBowAmmoDisplay();
            }
        }
    }

    private void DoUnarmedAttack()
    {
        IsAttacking = true;
        CanAttack = false;

        if (Random.value > 0.5f) animator.SetBool("PunchLeft", true);
        else animator.SetBool("PunchRight", true);

        AttackStart(); 
        StartCoroutine(AttackCooldownCoroutine(0.5f));
    }

    private IEnumerator SpearAttackRoutine(float cooldown)
    {
        IsAttacking = true;
        CanAttack = false;
        animator.Play("Player_Lance_Attack_Anim");
        animator.SetBool("isAttacking", true);
        
        yield return new WaitForSeconds(0.5f); // Або довжина анімації

        IsAttacking = false;
        if (lanceAttackZone != null) lanceAttackZone.Deactivate();
        animator.SetBool("isAttacking", false);
        StartCoroutine(AttackCooldownCoroutine(cooldown));
    }

    private IEnumerator AttackCooldownCoroutine(float time)
    {
        yield return new WaitForSeconds(time);
        CanAttack = true;
    }

    public void AttackStart()
    {
        AttackZone zone = GetCurrentAttackZone();
        if (zone != null) zone.Activate();
    }

    public void EndAttack()
    {
        AttackZone zone = GetCurrentAttackZone();
        if (zone != null) zone.Deactivate();

        animator.SetBool("isAttacking", false);
        animator.SetBool("PunchLeft", false);
        animator.SetBool("PunchRight", false);
        IsAttacking = false;
        player.OnActionFinished();
    }

    public void ResetCombatStates()
    {
        isCharging = false; // Важливо скинути зарядку при отриманні урону
        IsAttacking = false;
        IsShooting = false;
        animator.SetBool("isAttacking", false);
        animator.SetBool("isShooting", false);
        AttackZone zone = GetCurrentAttackZone();
        if (zone != null) zone.Deactivate();
    }

    private AttackZone GetCurrentAttackZone()
    {
        Item item = player.currentEquippedItem;
        if (item == null) return punchAttackZone;
        if (item.itemType == ItemType.Sword) return swordAttackZone;
        if (item.itemType == ItemType.Axe) return axeAttackZone;
        if (item.itemType == ItemType.Lance) return lanceAttackZone;
        return punchAttackZone;
    }
}