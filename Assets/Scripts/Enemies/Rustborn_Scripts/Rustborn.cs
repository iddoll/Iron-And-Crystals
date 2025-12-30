using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Rustborn : EnemyBase
{
    [Header("Rustborn Settings")]
    public float corrosionArmorMax = 30f;
    public float armorRegenOnHitPercent = 0.3f;
    public float regenDelayPartial = 2f;
    public float regenDelayBroken = 5f;
    public float armorRegenPerSecond = 2f;
    public float healthRegenPerSecond = 1f;

    private float corrosionArmorCurrent;
    private float lastHitTime;

    [Header("AI Settings")]
    public float followRadius = 6f;
    public float stopDistance = 1.2f;
    private Transform player;

    [Header("Patrol Settings")]
    public Transform leftPatrolPoint;
    public Transform rightPatrolPoint;
    private bool movingRight = true;

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

    // нова змінна
    private bool isAttacking = false;

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

        if (playerInAttackZone)
        {
            rb.linearVelocity = Vector2.zero;

            if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
            {
                TryAttack();
            }
        }
        else if (!isAttacking && playerInFollowRange)
        {
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            float targetSpeed = dirX * moveSpeed;
            float speed = Mathf.Lerp(rb.linearVelocity.x, targetSpeed, 0.1f);
            rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
            HandleFacing(dirX);
        }
        else if (!isAttacking)
        {
            Patrol();
        }

        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.05f;
        animator.SetBool(IsMovingHash, isMoving);

        HandleRegeneration();
    }

    private void Patrol()
    {
        if (leftPatrolPoint == null || rightPatrolPoint == null) return;

        if (movingRight)
        {
            rb.linearVelocity = new Vector2(moveSpeed, rb.linearVelocity.y);
            HandleFacing(1);

            if (transform.position.x >= rightPatrolPoint.position.x)
                movingRight = false;
        }
        else
        {
            rb.linearVelocity = new Vector2(-moveSpeed, rb.linearVelocity.y);
            HandleFacing(-1);

            if (transform.position.x <= leftPatrolPoint.position.x)
                movingRight = true;
        }
    }

    private void TryAttack()
    {
        lastAttackTime = Time.time;
        isAttacking = true;
        animator.SetBool(IsAttackingHash, true);
    }

    // Animation Event
    public void AE_DealDamage()
    {
        if (attackZone != null && attackZone.playerInZone && attackZone.player != null)
        {
            PlayerController.Instance?.TakeDamage(attackDamage);
        }
    }

    // Animation Event
    public void AE_AttackFinished()
    {
        animator.SetBool(IsAttackingHash, false);
        isAttacking = false;
    }

    private void HandleFacing(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.05f) return;
        float sign = Mathf.Sign(dirX);
        transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * sign, initialScale.y, initialScale.z);
    }

    // Додаємо параметр DamageType type
    public override void TakeDamage(float amount, DamageType type = DamageType.Melee)
    {
        lastHitTime = Time.time;

        if (corrosionArmorCurrent > 0)
        {
            corrosionArmorCurrent -= amount;

            if (corrosionArmorCurrent < 0)
            {
                float leftover = -corrosionArmorCurrent;
                corrosionArmorCurrent = 0;
                // Передаємо обидва параметри далі в базу
                base.TakeDamage(leftover, type); 
            }
            else
            {
                float regen = amount * armorRegenOnHitPercent;
                corrosionArmorCurrent = Mathf.Min(corrosionArmorCurrent + regen, corrosionArmorMax);
            }
        }
        else
        {
            // Передаємо обидва параметри далі в базу
            base.TakeDamage(amount, type);
        }
    }

    public bool HasArmor()
    {
        return corrosionArmorCurrent > 0;
    }
    
    private void HandleRegeneration()
    {
        float delay = (corrosionArmorCurrent > 0) ? regenDelayPartial : regenDelayBroken;

        if (Time.time - lastHitTime < delay)
            return;

        if (corrosionArmorCurrent < corrosionArmorMax)
        {
            corrosionArmorCurrent += armorRegenPerSecond * Time.fixedDeltaTime;
            corrosionArmorCurrent = Mathf.Min(corrosionArmorCurrent, corrosionArmorMax);
        }

        if (currentHealth < maxHealth)
        {
            currentHealth += healthRegenPerSecond * Time.fixedDeltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
    }
    protected override void Die()
    {
        // 🛠️ Від'єднуємо всі стріли, що застрягли у ворогу, перед його знищенням
        DetachArrows();
        
        // Викликаємо стандартну логіку смерті з EnemyBase
        base.Die();
    }

    private void DetachArrows()
    {
        // Проходимо по всіх дочірніх об'єктах у цьому об'єкті
        // Створюємо список, щоб уникнути помилок під час зміни ієрархії
        var children = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            // Перевіряємо, чи є на дочірньому об'єкті скрипт Arrow
            if (child.GetComponent<Arrow>() != null)
            {
                // Від'єднуємо стрілу від ворога.
                // Вона стане об'єктом верхнього рівня в ієрархії.
                child.parent = null;
            }
        }
    }
    public float GetArmor01() => corrosionArmorCurrent / corrosionArmorMax;
}
