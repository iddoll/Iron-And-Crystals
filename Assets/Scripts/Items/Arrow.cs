using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float lifeTime = 3f;

    private Rigidbody2D rb;

    // Викликається після спавну, щоб задати напрямок польоту
    public void Shoot(bool facingRight)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Летить по прямій
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        rb.linearVelocity = direction * speed;

        // Поворот стріли під напрямок руху
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Перевіряємо, чи обʼєкт має EnemyBase
        EnemyBase enemy = collision.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage); // завдаємо шкоди
            Destroy(gameObject);
            return;
        }

        // Знищення при зіткненні з землею
        if (collision.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}