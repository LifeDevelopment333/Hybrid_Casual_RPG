using TMPro;
using UnityEngine;

public class FloatingDamageText : MonoBehaviour
{
    private TMP_Text tmpText;
    private Camera cam;
    private RectTransform rect;

    private float elapsedTime = 0f;
    private float duration = 0.5f;

    public void Initialize()
    {
        tmpText = GetComponent<TMP_Text>();
        rect = GetComponent<RectTransform>();
        cam = Camera.main;
    }

    public void Show(DamageResult result, RectTransform canvasRect, Vector3 position)
    {
        tmpText.SetText(result.Amount.ToString());

        Vector3 screenPosition = cam.WorldToScreenPoint(position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 startPosition);
        rect.anchoredPosition = startPosition;

        elapsedTime = 0;

        gameObject.SetActive(true);
    }

    public bool Tick()
    {
        elapsedTime += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsedTime / duration);

        if(progress >= 1f)
        {
            return false;
        }

        rect.anchoredPosition += Vector2.up * 50 * Time.deltaTime;
        rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, progress * 3f);
        tmpText.alpha = 1f - Mathf.InverseLerp(0.4f, 1f, progress);

        return true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
