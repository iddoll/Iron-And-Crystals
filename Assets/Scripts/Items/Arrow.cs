using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public float speed = 15f; // Підняв швидкість, бо з гравітацією вона летітиме дугою
    public int damage = 10;
    public float gravityMultiplier = 1f; // Наскільки сильно гравітація впливає на стрілу

    private Rigidbody2D rb;
    private bool hasLanded = false;
    private Collider2D mainCollider;
    private Collider2D pickupTrigger;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length >= 2)
        {
            mainCollider = colliders[0];
            pickupTrigger = colliders[1];
        }
    }
    
    public void Shoot(bool facingRight)
    {
        // 1. Вмикаємо гравітацію, щоб стріла падала
        rb.gravityScale = gravityMultiplier;
        
        // 2. Додаємо початковий імпульс (можна трохи вгору, щоб летіла далі)
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        // Додамо невеликий нахил вгору (наприклад, 0.1 по Y), щоб був ефект балістики
        direction += new Vector2(0, 0.1f); 
        
        rb.linearVelocity = direction.normalized * speed;
        
        if (pickupTrigger != null) pickupTrigger.enabled = false;
    }

    void Update()
    {
        // 3. Постійно розвертаємо стрілу за вектором її швидкості
        if (!hasLanded)
        {
            RotateToVelocity();
        }
    }
    
    private void RotateToVelocity()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            // Вираховуємо кут на основі поточної швидкості (куди летить, туди й дивиться)
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || hasLanded) return;

        EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            float finalDamage = damage;
            if (enemy is Rustborn rustborn && rustborn.HasArmor())
            {
                finalDamage *= 0.5f;
            }
            enemy.TakeDamage(finalDamage, DamageType.Projectile);
        }

        StopArrow(collision.transform);
    }

    private void StopArrow(Transform target)
    {
        hasLanded = true;
        
        // Вимикаємо фізику повністю
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
    
        transform.SetParent(target);

        if (mainCollider != null) mainCollider.enabled = false;
        if (pickupTrigger != null) pickupTrigger.enabled = true;
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasLanded) return;

        EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            // 👻 Прибираємо логіку з привидом, оскільки він не отримує шкоди від стріл
            Nocktal nocktal = enemy.GetComponent<Nocktal>();
            if (nocktal != null)
            {
                 Debug.Log("Стріла пройшла крізь привида!");
                 // Стріла не знищується і не наносить урону
            }
        }
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (hasLanded && collision.CompareTag("Player"))
        {
            // Логіка підказки інтерфейсу тут
        }
    }
}