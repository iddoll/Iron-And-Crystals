using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public Image foregroundImage;
    public Image armorImage;
    private EnemyBase enemy;

    void Start()
    {
        enemy = GetComponentInParent<EnemyBase>(); // беремо базовий клас
    }

    void Update()
    {
        if(enemy != null)
        {
            foregroundImage.fillAmount = enemy.CurrentHealthNormalized;

            if (enemy is Rustborn rustborn) // доступ саме до Rustborn
            {
                if (armorImage != null)
                    armorImage.fillAmount = rustborn.GetArmor01();
            }
        }

        transform.rotation = Quaternion.LookRotation(
            transform.position - Camera.main.transform.position
        );
    }
}