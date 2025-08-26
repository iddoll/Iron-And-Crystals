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

        // Логіка атаки та руху
        if (playerInAttackZone)
        {
            // Якщо гравець у зоні атаки, зупиняємо рух і починаємо/продовжуємо атакувати
            isMoving = false;
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                TryAttack();
            }
        }
        else if (!isCurrentlyAttacking && playerInFollowRange)
        {
            // Рух до гравця, якщо він у радіусі, але не в зоні атаки
            if (Vector2.Distance(transform.position, player.position) > stopDistance)
            {
                Vector2 direction = (player.position - transform.position).normalized;
                targetPosition += direction * moveSpeed * Time.fixedDeltaTime;
                isMoving = true;
            }
        }
        
        // Вертикальне коливання завжди застосовується
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        targetPosition.y = Mathf.Lerp(rb.position.y, startPosition.y + yOffset, verticalLerp);
        
        // Застосовуємо рух
        rb.MovePosition(targetPosition);

        // Оновлюємо анімацію
        animator.SetBool(IsMovingHash, isMoving);

        // Поворот (завжди, якщо в радіусі слідування)
        if (playerInFollowRange)
        {
            HandleFacing(player.position.x - transform.position.x);
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