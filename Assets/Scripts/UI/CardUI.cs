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

    [SerializeField] private RectTransform playAnimationTarget;

    public float HoverProgress { get; private set; }
    public bool IsHovered { get; private set; }
    public bool IsDragging { get; private set; }

    public Vector2 TargetPosition;
    public Quaternion TargetRotation;
    public Vector3 TargetScale = Vector3.one;

    private RectTransform rectTransform;

    private CardData cardData;
    public CardData Card => cardData;
    private CardActionAnimation cardActionAnimation;
    private PlayerHand playerHand;
    private PlayerHandUI handUI;
    private PlayZone playZone;
    private PlayerResource playerResource;
    private CardManager cardManager;

    private RectTransform handParentRect;

    private Vector2 dragOffset;
    private Vector2 dragTargetLocalPosition;

    public bool isAnimating;

    public void Initialize(CardData card, PlayerHand hand)
    {
        cardData = card;
        playerHand = hand;

        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = card.description;

        if (rectTransform != null)
        {
            TargetPosition = rectTransform.anchoredPosition;
            TargetRotation = rectTransform.localRotation;
            TargetScale = rectTransform.localScale;
        }

        // Ensure interactive
        CanvasGroup cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    void Awake()
    {
        cardActionAnimation = GetComponent<CardActionAnimation>();
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            TargetPosition = rectTransform.anchoredPosition;
            TargetRotation = rectTransform.localRotation;
            TargetScale = rectTransform.localScale;
        }

        handUI = FindFirstObjectByType<PlayerHandUI>();
        playZone = FindFirstObjectByType<PlayZone>();
        playerResource = FindFirstObjectByType<PlayerResource>();
        cardManager = FindFirstObjectByType<CardManager>();
        playerHand = FindFirstObjectByType<PlayerHand>();

        if (transform.parent != null)
            handParentRect = transform.parent as RectTransform;

        if (playAnimationTarget == null && playZone != null && playZone.transform.childCount > 0)
            playAnimationTarget = playZone.transform.GetChild(0).GetComponent<RectTransform>();
    }

    void Update()
    {
        float targetHover = (IsHovered && !IsDragging && !isAnimating) ? 1f : 0f;

        HoverProgress = Mathf.MoveTowards(
            HoverProgress,
            targetHover,
            Time.deltaTime * hoverSmoothSpeed
        );
    }

    void LateUpdate()
    {
        if (isAnimating)
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
        if (IsDragging || isAnimating)
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

        bool canExecute = cardManager != null && cardManager.CanExecute(cardData);
        bool overPlayZone = IsOverPlayZone(eventData);

        if (overPlayZone && canExecute)
        {
            if (cardActionAnimation == null)
            {
                IsDragging = false;
                return;
            }

            isAnimating = true;
            IsDragging = false;
            IsHovered = false;

            // Capture locals
            CardData localCard = cardData;
            PlayerHand localPlayerHand = playerHand != null ? playerHand : FindFirstObjectByType<PlayerHand>();
            PlayerHandUI localHandUI = handUI != null ? handUI : FindFirstObjectByType<PlayerHandUI>();
            GameObject visualObject = this.gameObject;
            RectTransform animationParent = handParentRect != null ? handParentRect : (rectTransform.parent as RectTransform);

            // Start animation and handle completion
            StartCoroutine(cardActionAnimation.AnimateTo(
                animationParent,
                playAnimationTarget,
                () =>
                {
                    // Update game logic
                    if (localPlayerHand != null)
                        localPlayerHand.PlayCardFromHand(localCard);

                    // Remove the visual from the hand UI by reference
                    if (localHandUI != null)
                    {
                        localHandUI.RemoveCardObject(visualObject);
                    }
                    else
                    {
                        Destroy(visualObject);
                    }
                }));

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

    // Called by Reveal flow when we reparent the visual into the hand to update drag parent reference
    public void RefreshHandParentRect(RectTransform newParent)
    {
        handParentRect = newParent;
    }
}