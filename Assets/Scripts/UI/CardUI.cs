using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System;

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
    }

    void Awake()
    {
        cardActionAnimation = GetComponent<CardActionAnimation>();
        rectTransform = GetComponent<RectTransform>();

        handUI = FindFirstObjectByType<PlayerHandUI>();
        playZone = FindFirstObjectByType<PlayZone>();
        playerResource = FindFirstObjectByType<PlayerResource>();
        cardManager = FindFirstObjectByType<CardManager>();

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

        if (IsOverPlayZone(eventData) && cardManager.CanExecute(cardData))
        {
            isAnimating = true;
            IsDragging = false;
            IsHovered = false;

            StartCoroutine(cardActionAnimation.AnimateTo(
                handParentRect,
                playAnimationTarget,
                OnCardPlayed));
            return;
        }
        else
        {
            Debug.Log("cannot afford");
        }

        IsDragging = false;

        if (handUI != null)
            handUI.LayoutCards();
    }

    private void OnCardPlayed()
    {
        playerHand.PlayCardFromHand(cardData);
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
}