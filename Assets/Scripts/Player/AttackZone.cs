using UnityEngine;
using System.Collections.Generic; // Виправлено: використовуємо правильний простір імен

public class AttackZone : MonoBehaviour
{
    private HashSet<GameObject> enemiesHitInThisAttack = new HashSet<GameObject>();
    public LayerMask enemyLayer;

    public void Activate()
    {
        enemiesHitInThisAttack.Clear();
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayer) != 0 && !enemiesHitInThisAttack.Contains(other.gameObject))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                if (PlayerController.Instance.currentEquippedItem != null)
                {
                    PlayerController.Instance.DealArmedDamageToEnemy(enemy); 
                }
                else
                {
                    PlayerController.Instance.DealUnarmedDamageToEnemy(enemy);
                }
                
                enemiesHitInThisAttack.Add(other.gameObject);
                Debug.Log($"Ударили {enemy.enemyName}!");
            }
        }
    }
}