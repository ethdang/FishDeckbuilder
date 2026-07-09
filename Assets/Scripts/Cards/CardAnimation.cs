using System;
using System.Collections;
using UnityEngine;

public class CardActionAnimation : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.15f;
    [SerializeField] private float holdDuration = 0.35f;
    [SerializeField] private float fadeDuration = 0.20f;
    [SerializeField] private float enlargedScale = 1.35f;

    // A time scale applied to all internal durations. 1 = normal, 0.5 = twice as fast, 2 = half speed.
    public float timeScale = 1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // guard so AnimateTo/AnimateToNoFade don't overlap
    private bool isAnimating = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private IEnumerator RunMove(RectTransform parentRect, RectTransform target, bool doFade, Action onFinished)
    {
        if (isAnimating)
        {
            Debug.LogWarning($"[CardActionAnimation] Animation already running on {gameObject.GetInstanceID()}, skipping new request.");
            yield break;
        }

        isAnimating = true;
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;

        Vector2 startPosition = rectTransform.anchoredPosition;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null ? canvas.worldCamera : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.position);

        Vector2 targetPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out targetPosition);

        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = Vector3.one * enlargedScale;

        // Apply timeScale to durations
        float md = Mathf.Max(0.001f, moveDuration * timeScale);
        float hd = Mathf.Max(0f, holdDuration * timeScale);
        float fd = Mathf.Max(0.001f, fadeDuration * timeScale);

        float timer = 0f;
        // Move
        while (timer < md)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / md);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            rectTransform.localRotation = Quaternion.identity;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = targetScale;
        rectTransform.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(hd);

        if (doFade)
        {
            timer = 0f;
            Vector3 endScale = targetScale * 0.5f;
            while (timer < fd)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / fd);
                rectTransform.localScale = Vector3.Lerp(targetScale, endScale, t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
        }

        try { onFinished?.Invoke(); } catch (Exception ex) { Debug.LogError(ex); }

        // restore interactivity so the object can be clicked again if needed (caller can still destroy)
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        isAnimating = false;
    }

    public IEnumerator AnimateTo(RectTransform parentRect, RectTransform target, Action onFinished = null)
    {
        yield return StartCoroutine(RunMove(parentRect, target, true, () =>
        {
            onFinished?.Invoke();
        }));
    }

    public IEnumerator AnimateToNoFade(RectTransform parentRect, RectTransform target, Action onFinished = null)
    {
        yield return StartCoroutine(RunMove(parentRect, target, false, () =>
        {
            onFinished?.Invoke();
        }));
    }
}