using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    public string enemyName = "Enemy";
    public float maxHealth = 100f;
    public float moveSpeed = 2f;
    public float attackDamage = 10f;
    
    protected float currentHealth;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float amount)
    {
        currentHealth -= amount;
        Debug.Log($"{enemyName} отримав {amount} урону. Поточне HP: {currentHealth}");
        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
