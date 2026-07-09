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

    [SerializeField] private RectTransform revealTarget;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();

        revealTarget = FindFirstObjectByType<PlayZone>().GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // Existing behavior: move -> hold -> fade out -> onFinished
    public IEnumerator AnimateTo(
        RectTransform parentRect,
        RectTransform target,
        Action onFinished = null)
    {
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;

        Vector2 startPosition = rectTransform.anchoredPosition;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null ? canvas.worldCamera : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            cam,
            target.position
        );

        Vector2 targetPosition = revealTarget.position;

        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = Vector3.one * enlargedScale;

        float timer = 0f;

        // Move to target
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);

            rectTransform.anchoredPosition =
                Vector2.Lerp(startPosition, targetPosition, t);

            rectTransform.localScale =
                Vector3.Lerp(startScale, targetScale, t);

            rectTransform.localRotation = Quaternion.identity;

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = targetScale;
        rectTransform.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(holdDuration);

        // Fade out
        timer = 0f;
        Vector3 endScale = targetScale * 0.5f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / fadeDuration);

            rectTransform.localScale =
                Vector3.Lerp(targetScale, endScale, t);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        onFinished?.Invoke();
    }

    // New: Move to target and hold, but do NOT fade out.
    // Useful for reveal flows where we want to keep the card visible and then animate it back.
    public IEnumerator AnimateToNoFade(
        RectTransform parentRect,
        RectTransform target,
        Action onFinished = null)
    {
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 1f;

        Vector2 startPosition = rectTransform.anchoredPosition;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null ? canvas.worldCamera : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            cam,
            target.position
        );

        Vector2 targetPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPoint,
            cam,
            out targetPosition
        );

        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = Vector3.one * enlargedScale;

        float timer = 0f;

        // Move to target
        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);

            rectTransform.anchoredPosition =
                Vector2.Lerp(startPosition, targetPosition, t);

            rectTransform.localScale =
                Vector3.Lerp(startScale, targetScale, t);

            rectTransform.localRotation = Quaternion.identity;

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = targetScale;
        rectTransform.localRotation = Quaternion.identity;

        // Hold at target, using same holdDuration value
        yield return new WaitForSeconds(holdDuration);

        // Do not fade; just call callback
        onFinished?.Invoke();
    }
}