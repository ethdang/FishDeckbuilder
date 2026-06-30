using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CardUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Hover")]
    [SerializeField] private float hoverSmoothSpeed = 12f;

    [Header("Animation")]
    [SerializeField] private float animationSpeed = 15f;

    [Header("Drag")]
    [SerializeField] private float dragScaleBoost = 0.14f;
    [SerializeField] private float dragStraightenAmount = 0.75f;

    [Header("Play Animation")]
    [SerializeField] private float playMoveDuration = 0.15f;
    [SerializeField] private float playHoldDuration = 0.35f;
    [SerializeField] private float playFadeDuration = 0.20f;
    [SerializeField] private float playScale = 1.35f;
    [SerializeField] private RectTransform playAnimationTarget;

    public float HoverProgress { get; private set; }
    public bool IsHovered { get; private set; }
    public bool IsDragging { get; private set; }
    public bool IsPlayingCard => isPlayingCard;

    public Vector2 TargetPosition;
    public Quaternion TargetRotation;
    public Vector3 TargetScale = Vector3.one;

    private CardData cardData;
    private PlayerHand playerHand;
    private PlayerHandUI handUI;
    private PlayZone playZone;

    private RectTransform rectTransform;
    private RectTransform handParentRect;
    private CanvasGroup canvasGroup;

    private Vector2 dragOffset;
    private Vector2 dragTargetLocalPosition;

    private bool isPlayingCard;

    public void Initialize(CardData card, PlayerHand hand)
    {
        cardData = card;
        playerHand = hand;

        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = card.description;
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        handUI = FindFirstObjectByType<PlayerHandUI>();
        playZone = FindFirstObjectByType<PlayZone>();

        if (transform.parent != null)
            handParentRect = transform.parent as RectTransform;

        if (playAnimationTarget == null && playZone != null && playZone.transform.childCount > 0)
            playAnimationTarget = playZone.transform.GetChild(0).GetComponent<RectTransform>();
    }

    void Update()
    {
        float targetHover = (IsHovered && !IsDragging && !isPlayingCard) ? 1f : 0f;

        HoverProgress = Mathf.MoveTowards(
            HoverProgress,
            targetHover,
            Time.deltaTime * hoverSmoothSpeed
        );
    }

    void LateUpdate()
    {
        if (isPlayingCard)
            return;

        float t = 1f - Mathf.Exp(-animationSpeed * Time.deltaTime);

        if (IsDragging)
        {
            rectTransform.anchoredPosition = dragTargetLocalPosition;

            rectTransform.localRotation = Quaternion.Slerp(
                rectTransform.localRotation,
                Quaternion.identity,
                Mathf.Clamp01(dragStraightenAmount) * t
            );

            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                TargetScale * (1f + dragScaleBoost),
                t
            );

            transform.SetAsLastSibling();
            return;
        }

        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            TargetPosition,
            t
        );

        rectTransform.localRotation = Quaternion.Slerp(
            rectTransform.localRotation,
            TargetRotation,
            t
        );

        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            TargetScale,
            t
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsDragging || isPlayingCard)
            return;

        IsHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsHovered = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardData == null || playerHand == null || handParentRect == null)
            return;

        IsDragging = true;
        IsHovered = false;

        Vector2 localPointer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handParentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPointer
        );

        dragOffset = rectTransform.anchoredPosition - localPointer;
        dragTargetLocalPosition = rectTransform.anchoredPosition;

        transform.SetAsLastSibling();

        if (handUI != null)
            handUI.LayoutCards();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        UpdateDragPosition(eventData);
        transform.SetAsLastSibling();
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        if (handParentRect == null)
            return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handParentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        dragTargetLocalPosition = localPoint + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        if (IsOverPlayZone(eventData))
        {
            isPlayingCard = true;
            IsDragging = false;
            IsHovered = false;

            if (handUI != null)
                handUI.RemoveCardObject(gameObject);

            StartCoroutine(PlayCardAnimation());
            return;
        }

        IsDragging = false;

        if (handUI != null)
            handUI.LayoutCards();
    }

    private bool IsOverPlayZone(PointerEventData eventData)
    {
        if (playZone == null || playAnimationTarget == null)
            return false;

        RectTransform rect = playZone.GetComponent<RectTransform>();

        return RectTransformUtility.RectangleContainsScreenPoint(
            rect,
            eventData.position,
            eventData.pressEventCamera
        );
    }

    private IEnumerator PlayCardAnimation()
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
            playAnimationTarget.position
        );

        Vector2 centerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            handParentRect,
            screenPoint,
            cam,
            out centerPosition
        );

        Vector3 startScale = rectTransform.localScale;
        Vector3 enlargedScale = Vector3.one * playScale;

        float timer = 0f;

        while (timer < playMoveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / playMoveDuration);

            rectTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                centerPosition,
                t
            );

            rectTransform.localScale = Vector3.Lerp(
                startScale,
                enlargedScale,
                t
            );

            rectTransform.localRotation = Quaternion.identity;

            yield return null;
        }

        rectTransform.anchoredPosition = centerPosition;
        rectTransform.localScale = enlargedScale;
        rectTransform.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(playHoldDuration);

        if (playerHand != null)
            playerHand.PlayCardFromHand(cardData);

        timer = 0f;
        Vector3 endScale = enlargedScale * 0.5f;

        while (timer < playFadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / playFadeDuration);

            rectTransform.localScale = Vector3.Lerp(
                enlargedScale,
                endScale,
                t
            );

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}