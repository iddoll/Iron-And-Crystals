using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Rustborn : EnemyBase
{
    [Header("Rustborn Settings")]
    public float corrosionArmorMax = 30f;         // Максимальна корозійна броня
    public float armorRegenOnHitPercent = 0.3f;   // Скільки % броні відновлює після удару (наприклад 0.3 = 30%)
    public float regenDelayPartial = 2f;  // коли броня ще залишилась
    public float regenDelayBroken = 5f;   // коли броня повністю вибита
    public float armorRegenPerSecond = 2f;        // Швидкість відновлення броні/сек
    public float healthRegenPerSecond = 1f;       // Швидкість відновлення хп/сек

    private float corrosionArmorCurrent;
    private float lastHitTime;

    [Header("AI Settings")]
    public float followRadius = 6f;
    public float stopDistance = 1.2f;
    private Transform player;

    [Header("Attack Settings")]
    public float attackCooldown = 2f;
    private float lastAttackTime;
    public EnemyAttackZone attackZone;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;
    private Vector3 initialScale;

    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int IsMovingHash = Animator.StringToHash("isMooving");

    protected override void Start()
    {
        base.Start();
        corrosionArmorCurrent = corrosionArmorMax;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        initialScale = transform.localScale;

        lastAttackTime = -attackCooldown;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        bool playerInFollowRange = Vector2.Distance(transform.position, player.position) <= followRadius;
        bool playerInAttackZone = attackZone != null && attackZone.playerInZone;

        Vector2 targetPosition = rb.position;
        bool isMoving = false;

        if (playerInAttackZone)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                TryAttack();
            }
        }
        else if (playerInFollowRange && Vector2.Distance(transform.position, player.position) > stopDistance)
        {
            Vector2 dir = (player.position - transform.position).normalized;
            targetPosition += dir * moveSpeed * Time.fixedDeltaTime;
            isMoving = true;
        }

        rb.MovePosition(targetPosition);
        animator.SetBool(IsMovingHash, isMoving);

        if (playerInFollowRange)
        {
            HandleFacing(player.position.x - transform.position.x);
        }

        HandleRegeneration();
    }

    private void TryAttack()
    {
        lastAttackTime = Time.time;
        animator.SetBool(IsAttackingHash, true);
    }

    public void AE_DealDamage()
    {
        if (attackZone != null && attackZone.playerInZone && attackZone.player != null)
        {
            PlayerController.Instance?.TakeDamage(attackDamage);
        }
    }

    public void AE_AttackFinished()
    {
        animator.SetBool(IsAttackingHash, false);
    }

    private void HandleFacing(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.05f) return;
        float sign = Mathf.Sign(dirX);
        transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * sign, initialScale.y, initialScale.z);
    }

    // === Броня / Регенерація ===
    public override void TakeDamage(float amount)
    {
        lastHitTime = Time.time;

        if (corrosionArmorCurrent > 0)
        {
            corrosionArmorCurrent -= amount;

            // якщо пробили броню
            if (corrosionArmorCurrent < 0)
            {
                float leftover = -corrosionArmorCurrent;
                corrosionArmorCurrent = 0;
                base.TakeDamage(leftover); // шкода йде в HP
            }
            else
            {
                // миттєве часткове відновлення броні (тільки якщо вона залишилась >0)
                float regen = amount * armorRegenOnHitPercent;
                corrosionArmorCurrent = Mathf.Min(corrosionArmorCurrent + regen, corrosionArmorMax);
            }
        }
        else
        {
            base.TakeDamage(amount);
        }
    }


    private void HandleRegeneration()
    {
        // Вибираємо потрібну затримку в залежності від стану броні
        float delay = (corrosionArmorCurrent > 0) ? regenDelayPartial : regenDelayBroken;

        if (Time.time - lastHitTime < delay) 
            return;

        // Реген броні
        if (corrosionArmorCurrent < corrosionArmorMax)
        {
            corrosionArmorCurrent += armorRegenPerSecond * Time.fixedDeltaTime;
            corrosionArmorCurrent = Mathf.Min(corrosionArmorCurrent, corrosionArmorMax);
        }

        // Реген HP
        if (currentHealth < maxHealth)
        {
            currentHealth += healthRegenPerSecond * Time.fixedDeltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
    }


    // для UI, щоб можна було показати 2 смужки (hp + броня)
    public float GetArmor01() => corrosionArmorCurrent / corrosionArmorMax;
}
