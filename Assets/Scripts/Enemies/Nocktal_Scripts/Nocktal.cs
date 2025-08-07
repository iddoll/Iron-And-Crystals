using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Nocktal : EnemyBase
{
    [Header("Ghost Settings")]
    public float floatAmplitude = 0.2f;  // амплітуда вертикального коливання
    public float floatFrequency = 2f;    // швидкість коливання

    [Header("AI Settings")]
    public float followRadius = 5f;      // радіус, в якому починає слідувати
    public float stopDistance = 0.5f;    // відстань до гравця, на якій зупиняється
    [Range(0f, 1f)] public float verticalLerp = 0.7f; // плавність y офсету

    [Header("Sprite / Facing")]
    public bool spriteFacesRightByDefault = true; // якщо спрайт "лицем" дивиться вправо — true, інакше false
    public bool useFlipXInsteadOfScale = true;    // зручніше інколи використовувати SpriteRenderer.flipX

    private Transform player;
    private Vector3 startPosition;
    private Vector3 initialScale;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator animator;

    // Animator param name
    private static readonly int IsMovingHash = Animator.StringToHash("isMooving");

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        startPosition = transform.position;
        initialScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Якщо Rigidbody у Dynamic і ми керуємо через MovePosition, краще зробити interpolation
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        // Якщо привид повинен пролітати крізь об'єкти — можна ставити bodyType = Kinematic
        // rb.bodyType = RigidbodyType2D.Kinematic; // опціонально
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Вертикальна хвиля відносно стартової позиції
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        float targetY = startPosition.y + yOffset;

        // Якщо гравець у радіусі — переходимо в режим слідування
        if (dist <= followRadius)
        {
            // Визначаємо позицію до якої рухатися
            Vector2 direction = (player.position - transform.position);
            float distanceToKeep = Mathf.Max(stopDistance, 0f);

            Vector2 moveTarget;
            if (direction.magnitude > distanceToKeep)
            {
                Vector2 dirNorm = direction.normalized;
                moveTarget = (Vector2)transform.position + dirNorm * moveSpeed * Time.fixedDeltaTime;
            }
            else
            {
                // Залишатися на місці (але застосувати вертикальний офсет)
                moveTarget = transform.position;
            }

            // Застосуємо вертикальний офсет плавно
            moveTarget.y = Mathf.Lerp(transform.position.y, targetY, verticalLerp);

            rb.MovePosition(moveTarget);

            // Анімація руху
            if (animator != null) animator.SetBool(IsMovingHash, true);

            // Поворот/фліп в бік гравця
            HandleFacing(player.position.x - transform.position.x);
        }
        else
        {
            // Поза радіусом — Idle: лишаємось/плаваєм на місці
            Vector2 idlePos = transform.position;
            idlePos.y = Mathf.Lerp(transform.position.y, targetY, verticalLerp);
            rb.MovePosition(idlePos);

            if (animator != null) animator.SetBool(IsMovingHash, false);
        }
    }

    private void HandleFacing(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.05f) return;

        float sign = Mathf.Sign(dirX);

        // Якщо спрайт у вихідному вигляді "лицем" вправо, то при dirX>0 потрібно показувати "право"
        // Але у тебе спрайт може бути намальований вліво — врахуй це:
        if (!spriteFacesRightByDefault)
            sign = -sign; // інвертуємо логіку, якщо спрайт "лицем" дивиться вліво по замовчуванню

        if (useFlipXInsteadOfScale && sr != null)
        {
            sr.flipX = sign < 0f ? true : false;
        }
        else
        {
            transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * sign, initialScale.y, initialScale.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log(enemyName + " торкнувся гравця!");
            // TODO: викликати Player.TakeDamage() або інший ефект
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Візуалізація радіусу слідування
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, followRadius);

        // Візуалізація зони зупинки
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}
