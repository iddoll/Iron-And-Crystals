using UnityEngine;
using UnityEngine.UI;

public class UICursor : MonoBehaviour
{
    [Header("Cursor Sprites")]
    public Image cursorImage;        // посилання на Image
    public Sprite defaultCursor;     // звичайний курсор
    public Sprite attackCursor;      // при наведенні на ворога
    public Sprite clickCursor;       // при натисканні кнопки

    private RectTransform rectTransform;
    private Camera mainCam;

    private bool isClicking = false; // прапорець стану
    private bool isCustomEnabled = true;

    private void Start()
    {
        rectTransform = cursorImage.GetComponent<RectTransform>();
        mainCam = Camera.main;

        SetCursor(defaultCursor);
        EnableCustomCursor(); // на старті вмикаємо кастомний
    }

    private void Update()
    {
        if (!isCustomEnabled) return; // якщо інвентар відкритий — курсор не оновлюється

        // рухаємо UI-курсор за мишею
        rectTransform.position = Input.mousePosition;

        // перевірка кліку
        if (Input.GetMouseButtonDown(0))
        {
            isClicking = true;
            SetCursor(clickCursor);
        }
        if (Input.GetMouseButtonUp(0))
        {
            isClicking = false;
            UpdateCursorState();
        }

        if (!isClicking)
        {
            UpdateCursorState();
        }
    }

    private void UpdateCursorState()
    {
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        if (hit.collider != null && hit.collider.CompareTag("Enemy"))
        {
            SetCursor(attackCursor);
            return;
        }

        SetCursor(defaultCursor);
    }

    public void SetCursor(Sprite newCursor)
    {
        if (cursorImage != null && newCursor != null)
            cursorImage.sprite = newCursor;
    }

    // --- Методи для перемикання ---
    public void EnableCustomCursor()
    {
        isCustomEnabled = true;
        cursorImage.enabled = true;
        Cursor.visible = false;
    }

    public void DisableCustomCursor()
    {
        isCustomEnabled = false;
        cursorImage.enabled = false;
        Cursor.visible = true;
    }
}
