using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Prefabs & Sprites")]
    public GameObject heartPrefab; // Префаб серця
    public Transform heartsParent; // Контейнер
    public Sprite fullHeart;
    public Sprite emptyHeart;

    private List<Image> hearts = new List<Image>();
    private int lastHealth;

    public void InitHearts(int maxHearts)
    {
        foreach (Transform child in heartsParent) Destroy(child.gameObject);
        hearts.Clear();

        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, heartsParent);
            Image heartImage = heartObj.GetComponent<Image>();
            heartImage.sprite = fullHeart;
            heartImage.color = Color.white;
            hearts.Add(heartImage);
        }

        lastHealth = maxHearts;
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        int totalHearts = hearts.Count;
        int healthPerHeart = maxHealth / totalHearts;

        // Тремтіння сердечок тільки при втраті здоров'я
        if (currentHealth < lastHealth)
        {
            // Оновлюємо спрайти сердечок перед тряскою
            for (int i = 0; i < totalHearts; i++)
            {
                int heartHealth = (i + 1) * healthPerHeart;
                hearts[i].sprite = currentHealth >= heartHealth ? fullHeart : emptyHeart;
            }

            // Запускаємо тремтіння для всіх сердечок
            foreach (var heart in hearts)
            {
                heart.transform.DOShakeScale(0.3f, 0.5f, 8, 90, false);
            }
        }
        else
        {
            // Якщо HP збільшилось, просто оновлюємо спрайти без анімації
            for (int i = 0; i < totalHearts; i++)
            {
                int heartHealth = (i + 1) * healthPerHeart;
                hearts[i].sprite = currentHealth >= heartHealth ? fullHeart : emptyHeart;
            }
        }
        
        lastHealth = currentHealth;
    }
}