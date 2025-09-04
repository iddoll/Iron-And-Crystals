using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using System.Collections;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Prefabs & Sprites")]
    public GameObject heartPrefab;
    public Transform heartsParent;
    public Sprite fullHeart;
    public Sprite emptyHeart;
    public Sprite whiteHeart;

    [Header("Low HP FX")]
    [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;
    public Image screenOverlay;

    private List<Image> hearts = new List<Image>();
    private int lastHealth;
    private Tween overlayTween;
    private bool isLowHealth = false;

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

        lastHealth = maxHearts;

        if (screenOverlay)
            screenOverlay.color = new Color(1, 0, 0, 0);
    }

    public void UpdateHearts(int currentHealth, int maxHealth)
    {
        int totalHearts = hearts.Count;
        int healthPerHeart = maxHealth / totalHearts;

        // Завжди оновлюємо спрайти сердець
        for (int i = 0; i < totalHearts; i++)
        {
            int heartHealth = (i + 1) * healthPerHeart;
            hearts[i].sprite = currentHealth >= heartHealth ? fullHeart : emptyHeart;
        }

        // Якщо втратили HP → блимання (без постійної тряски)
        if (currentHealth < lastHealth)
        {
            for (int i = 0; i < totalHearts; i++)
            {
                if (hearts[i].sprite == fullHeart)
                {
                    hearts[i].transform.DOShakeScale(0.3f, 0.5f, 8, 90, false);
                    StartCoroutine(BlinkHeart(hearts[i]));
                }
            }
        }

        // Логіка для низького здоров'я
        bool currentIsLowHealth = currentHealth <= maxHealth * lowHealthThreshold;

        if (currentIsLowHealth && !isLowHealth)
        {
            StartLowHealthFX();
            isLowHealth = true;
        }
        else if (!currentIsLowHealth && isLowHealth)
        {
            StopLowHealthFX();
            isLowHealth = false;
        }

        lastHealth = currentHealth;
    }

    private IEnumerator BlinkHeart(Image heart)
    {
        // 2 рази перемикаємо спрайт
        for (int i = 0; i < 2; i++)
        {
            heart.sprite = whiteHeart;
            yield return new WaitForSeconds(0.2f);
            heart.sprite = fullHeart;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void StartLowHealthFX()
    {
        // Постійна тряска для всіх "живих" сердець
        foreach (var heart in hearts)
        {
            // Перевіряємо, чи серце "живе" і чи анімація вже запущена
            if (heart.sprite == fullHeart && !DOTween.IsTweening(heart.transform))
            {
                heart.transform.DOShakePosition(1f, 3f, 5, 90, false, true).SetLoops(-1).SetId(heart);
            }
        }

        // Червоне пульсуюче підсвічування екрану
        if (screenOverlay && overlayTween == null)
        {
            overlayTween = screenOverlay.DOFade(0.25f, 0.7f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    private void StopLowHealthFX()
    {
        // Зупиняємо анімації тільки для "живих" сердець
        foreach (var heart in hearts)
        {
            DOTween.Kill(heart);
        }

        // Зупиняємо анімацію підсвічування
        if (overlayTween != null)
        {
            overlayTween.Kill();
            overlayTween = null;
        }

        // Робимо підсвічування прозорим
        if (screenOverlay)
            screenOverlay.color = new Color(1, 0, 0, 0);
    }
}