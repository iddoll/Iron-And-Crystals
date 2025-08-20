using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerHealthUI : MonoBehaviour
{
    public GameObject heartPrefab; // Префаб серця
    public Transform heartsParent; // Контейнер
    public Sprite fullHeart;
    public Sprite emptyHeart;

    private List<Image> hearts = new List<Image>();

    public void InitHearts(int maxHearts)
    {
        foreach (Transform child in heartsParent) Destroy(child.gameObject);
        hearts.Clear();

        for (int i = 0; i < maxHearts; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, heartsParent);
            Image heartImage = heartObj.GetComponent<Image>();
            heartImage.sprite = fullHeart;
            hearts.Add(heartImage);
        }
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        int totalHearts = hearts.Count;
        int healthPerHeart = maxHealth / totalHearts;

        for (int i = 0; i < totalHearts; i++)
        {
            int heartHealth = (i + 1) * healthPerHeart;
            hearts[i].sprite = currentHealth >= heartHealth ? fullHeart : emptyHeart;
        }
    }
}
