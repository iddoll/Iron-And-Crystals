using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public Image foregroundImage;
    private EnemyBase enemy;

    void Start()
    {
        enemy = GetComponentInParent<EnemyBase>(); // беремо базовий клас
    }

    void Update()
    {
        if(enemy != null)
        {
            // Для доступу до currentHealth зробимо геттер у EnemyBase
            foregroundImage.fillAmount = enemy.CurrentHealthNormalized;
        }

        // щоб health bar завжди дивився на камеру
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}