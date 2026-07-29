using UnityEngine;

public class DurationIconUI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;

    private RectTransform rectTransform;
    private Vector2 targetPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        targetPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed);
    }

    public void SetTargetPosition(Vector2 position)
    {
        targetPosition = position;
    }
}