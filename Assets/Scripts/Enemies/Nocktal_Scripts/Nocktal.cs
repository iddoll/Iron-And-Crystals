using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Nocktal : EnemyBase
{
    [Header("Ghost Settings")]
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 2f;

    [Header("AI Settings")]
    public float followRadius = 5f;
    public float stopDistance = 0.5f;
    [Range(0f, 1f)] public float verticalLerp = 0.7f;

    [Header("Sprite / Facing")]
    public bool spriteFacesRightByDefault = true;
    public bool useFlipXInsteadOfScale = true;

    [Header("Attack Settings")]
    public float attackCooldown = 1.2f;
    private float lastAttackTime;

    [Header("Attack Zone")]
    public EnemyAttackZone attackZone;
    public Transform attackZoneTransform;
    public float attackZoneLocalX = 0.8f;
    public float attackZoneLocalY = 0.0f;

    [Header("Regeneration Settings")]
    public float regenDelay = 3f;              
    public float regenPerSecond = 2f;          
    private float lastHitTime;                 

    // 👻 Прибираємо isMaterialized, оскільки привид завжди нематеріальний
    private Collider2D mainCollider;
    
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int IsMovingHash = Animator.StringToHash("isMooving");

    private Transform player;
    private Vector3 startPosition;
    private Vector3 initialScale;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        startPosition = transform.position;
        initialScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        mainCollider = GetComponent<Collider2D>();
        // 👻 Колайдер привида завжди є тригером
        if (mainCollider != null)
        {
            mainCollider.isTrigger = true;
        }

        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        lastAttackTime = -attackCooldown;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        bool isCurrentlyAttacking = animator.GetBool(IsAttackingHash);
        bool playerInFollowRange = Vector2.Distance(transform.position, player.position) <= followRadius;
        bool playerInAttackZone = attackZone != null && attackZone.playerInZone;

        // Початкова цільова позиція - поточна позиція
        Vector2 targetPosition = rb.position;
        bool isMoving = false;

        if (playerInAttackZone)
        {
            isMoving = false;
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                TryAttack();
            }
        }
        else if (playerInFollowRange) 
        {
            if (Vector2.Distance(transform.position, player.position) > stopDistance)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                targetPosition += direction * moveSpeed * Time.fixedDeltaTime;
                isMoving = true;
            }

            HandleFacing(player.position.x - transform.position.x);
        }
        else
        {
            isMoving = false;
        }

        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        targetPosition.y = startPosition.y + yOffset;
        
        rb.MovePosition(targetPosition);
        animator.SetBool(IsMovingHash, isMoving);
        HandleRegeneration();
    }
    
    // ... (решта методів, як HandleAI, HandleRegeneration тощо, можуть залишитися)

    // 👻 Прибираємо TakeDamage, оскільки привид не вразливий для стріл
    // Якщо ви хочете, щоб він все ще міг отримувати урон від чогось іншого,
    // залиште цей метод, але приберіть перевірку isMaterialized.

    // Додаємо параметр DamageType type
    public override void TakeDamage(float damage, DamageType type = DamageType.Melee)
    {
        // Передаємо тип далі в базовий метод
        base.TakeDamage(damage, type);
        lastHitTime = Time.time;
    }
    // ... (решта ваших методів, як HandleFacing, UpdateAttackZoneSide тощо)
    private void HandleRegeneration()
    {
        if (Time.time - lastHitTime < regenDelay) return;

        if (currentHealth < maxHealth)
        {
            currentHealth += regenPerSecond * Time.fixedDeltaTime;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }
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
            Debug.Log($"{enemyName} наніс урон по гравцю.");
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

        if (useFlipXInsteadOfScale && sr != null)
        {
            sr.flipX = sign < 0f;
        }
        else
        {
            transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * sign, initialScale.y, initialScale.z);
        }
        UpdateAttackZoneSide(sign);
    }
    
    private void UpdateAttackZoneSide(float sign)
    {
        if (attackZoneTransform == null) return;

        if (useFlipXInsteadOfScale)
        {
            attackZoneTransform.localPosition = new Vector3(
                Mathf.Abs(attackZoneLocalX) * Mathf.Sign(sign),
                attackZoneLocalY,
                attackZoneTransform.localPosition.z
            );
        }
    }
}