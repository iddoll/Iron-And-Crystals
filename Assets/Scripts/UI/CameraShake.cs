using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Shake(float duration, float strength)
    {
        // зупиняємо попередню тряску
        transform.DOKill();

        // робимо shake позиції
        transform.DOShakePosition(duration, strength, 10, 90, false, true);
    }
}