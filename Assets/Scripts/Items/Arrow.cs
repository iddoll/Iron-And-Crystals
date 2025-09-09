using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float lifeTime = 3f;

    private Rigidbody2D rb;

    private bool hasLanded = false;
    private Collider2D mainCollider;
    private Collider2D pickupTrigger;
    
    void Awake()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        if (colliders.Length >= 2)
        {
            mainCollider = colliders[0];
            pickupTrigger = colliders[1];
        } else if (colliders.Length == 1)
        {
            mainCollider = colliders[0];
            Debug.LogWarning("На префабі стріли є лише один колайдер! Будь ласка, додайте другий для функціоналу підбирання.");
        }
    }
    
    public void Shoot(bool facingRight)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        rb.linearVelocity = direction * speed;
        RotateToVelocity();
        if (pickupTrigger != null)
        {
            pickupTrigger.enabled = false;
        }
    }
    
    private void RotateToVelocity()
    {
        if (rb.linearVelocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }
        
        if (hasLanded) return;
        
        EnemyBase enemy = collision.gameObject.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            float finalDamage = damage;

            // 👻 Прибираємо логіку з привидом, оскільки він тепер завжди тригер
            if (enemy is Rustborn)
            {
                Rustborn rustborn = (Rustborn)enemy;
                if (rustborn.HasArmor())
                {
                    finalDamage *= 0.5f;
                }
                enemy.TakeDamage(finalDamage);
            }
            
            // Стріла застрягає
            rb.isKinematic = true;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            hasLanded = true;
            transform.parent = collision.transform;

            if (mainCollider != null) mainCollider.enabled = false;
            if (pickupTrigger != null) pickupTrigger.enabled = true;
            
            return;
        }

        // Логіка для зіткнення з іншими об'єктами
        rb.isKinematic = true;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        hasLanded = true;
        
        transform.parent = collision.transform;

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