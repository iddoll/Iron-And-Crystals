using UnityEngine;
using System.Collections.Generic;

public class AttackZone : MonoBehaviour
{
    private HashSet<GameObject> enemiesHitInThisAttack = new HashSet<GameObject>();
    public LayerMask enemyLayer;

    public void Activate()
    {
        enemiesHitInThisAttack.Clear(); // Очистити список ворогів для нової атаки
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Перевіряємо, чи об'єкт є ворогом та чи ми його ще не вдарили під час цієї атаки
        if (((1 << other.gameObject.layer) & enemyLayer) != 0 && !enemiesHitInThisAttack.Contains(other.gameObject))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                // Викликаємо метод у PlayerController для нанесення шкоди
                PlayerController.Instance.DealDamageToEnemy(enemy);
                enemiesHitInThisAttack.Add(other.gameObject);
                Debug.Log($"Вдарили {enemy.enemyName}!");
            }
        }
    }
}