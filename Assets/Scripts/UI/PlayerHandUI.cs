using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerHandUI : MonoBehaviour
{
    public List<GameObject> activeObjects = new List<GameObject>();

    [SerializeField] private RectTransform objectParent;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private RectTransform discardPile;
    [SerializeField] private RectTransform spawnPosition;

    [Header("Fan Spread")]
    [SerializeField] private float minTotalFanAngle = 20f;
    [SerializeField] private float maxTotalFanAngle = 55f;
    [SerializeField] private int cardsForMaxAngle = 10;

    [SerializeField] private float minRadius = 380f;
    [SerializeField] private float maxRadius = 520f;

    [Header("Hover Spread")]
    [SerializeField] private float hoverFanBoost = 14f;
    [SerializeField] private float hoverRadiusBoost = 35f;
    [SerializeField] private float hoverLift = 70f;
    [SerializeField] private float hoverScaleBoost = 0.14f;

    [Header("Smoothing")]
    [SerializeField] private float layoutSmoothSpeed = 14f;

    private PlayerHand playerHand;
    private PlayerDeck playerDeck;

    public bool isDiscarding;
    private float currentTotalFanAngle;
    private float currentRadius;

    void Awake()
    {
        playerHand = FindFirstObjectByType<PlayerHand>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();

        if (objectParent == null)
            Debug.LogError("PlayerHandUI: objectParent is not assigned in Inspector.", this);

        if (cardPrefab == null)
            Debug.LogError("PlayerHandUI: cardPrefab is not assigned in Inspector.", this);

        if (spawnPosition == null && objectParent != null)
            spawnPosition = objectParent;
    }

    // public void UpdateHand()
    // {
    //     foreach (GameObject gameObject in activeObjects)
    //     {
    //         if (gameObject != null)
    //             Destroy(gameObject);
    //     }

    //     activeObjects.Clear();

    //     foreach (CardData cardData in playerHand.currentCards)
    //     {
    //         GameObject newCard = Instantiate(cardPrefab, objectParent);
    //         activeObjects.Add(newCard);

    //         CardUI cardUI = newCard.GetComponent<CardUI>();
    //         cardUI.Initialize(cardData, playerHand);

    //         RectTransform rect = newCard.GetComponent<RectTransform>();
    //         rect.anchoredPosition = new Vector2(0f, -100f);
    //     }

    //     LayoutCards(true);
    // }

    public void AddCard(CardData card)
    {
        if (cardPrefab == null || objectParent == null)
        {
            Debug.LogError("PlayerHandUI.AddCard: cardPrefab or objectParent not assigned.");
            return;
        }

        GameObject obj = Instantiate(cardPrefab, objectParent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = spawnPosition != null ? spawnPosition.anchoredPosition : Vector2.zero;

        CardUI ui = obj.GetComponent<CardUI>();
        if (ui != null)
            ui.Initialize(card, playerHand);

        activeObjects.Add(obj);

        LayoutCards(false);
    }

    public void RemoveCard(CardData card)
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject go = activeObjects[i];
            if (go == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }

            CardUI ui = go.GetComponent<CardUI>();
            if (ui != null && ui.Card == card)
            {
                Destroy(go);
                activeObjects.RemoveAt(i);
                LayoutCards();
                return;
            }
        }
    }

    public void RemoveCardObject(GameObject cardObject)
    {
        if (cardObject == null)
        {
            Debug.LogWarning("PlayerHandUI.RemoveCardObject called with null.");
            return;
        }

        Debug.Log($"[PlayerHandUI] RemoveCardObject called for object {cardObject.GetInstanceID()}. activeObjects before: {activeObjects.Count}");

        bool removed = activeObjects.Remove(cardObject);

        if (!removed)
        {
            Debug.LogWarning($"[PlayerHandUI] RemoveCardObject: object not found in activeObjects: {cardObject.GetInstanceID()}", cardObject);
        }
        else
        {
            Destroy(cardObject);
            Debug.Log($"[PlayerHandUI] Removed visual object. activeObjects now: {activeObjects.Count}");
        }

        LayoutCards(false);
}

    void LateUpdate()
    {
        if (isDiscarding)
            return;

        LayoutCards(false);
    }

    public IEnumerator DiscardCardsAnimated(float stagger = 0.08f, bool addToDiscardOnFinish = true)
    {
        isDiscarding = true;

        // Use the top-most Canvas rect so positions are consistent
        Canvas rootCanvas = objectParent != null ? objectParent.GetComponentInParent<Canvas>() : null;
        RectTransform animationParent = rootCanvas != null ? (rootCanvas.transform as RectTransform) : objectParent;

        while (activeObjects.Count > 0)
        {
            GameObject obj = activeObjects[activeObjects.Count - 1];
            activeObjects.RemoveAt(activeObjects.Count - 1);

            if (obj == null)
            {
                yield return null;
                continue;
            }

            CardUI ui = obj.GetComponent<CardUI>();
            if (ui != null)
                ui.isAnimating = true;

            var anim = obj.GetComponent<CardActionAnimation>();
            if (anim != null)
            {
                // Capture local vars for the callback closure
                CardData cardData = ui != null ? ui.Card : null;
                GameObject objToDestroy = obj;

                // Start the animation without waiting for it to finish
                StartCoroutine(anim.AnimateTo(animationParent, discardPile, () =>
                {
                    // Called when this card's animation completes
                    if (addToDiscardOnFinish && cardData != null)
                    {
                        // Ensure playerDeck reference
                        if (playerDeck == null)
                            playerDeck = FindFirstObjectByType<PlayerDeck>();

                        if (playerDeck != null)
                            playerDeck.AddToDiscard(cardData);
                    }

                    // Destroy the card object after its animation finishes
                    Destroy(objToDestroy);
                }));
            }
            else
            {
                Debug.LogWarning("DiscardCardsAnimated: CardActionAnimation missing on card object.", obj);
                Destroy(obj);
            }

            // Stagger the start of the next card's animation (adjust to taste)
            yield return new WaitForSeconds(stagger);
        }

        // All animations started (they will still finish in background)
        isDiscarding = false;
    }

    public void LayoutCards(bool snap = false)
    {
        if (isDiscarding) return;

        int count = activeObjects.Count;

        if (count == 0)
            return;

        int hoveredIndex = -1;
        float hoveredProgress = 0f;

        for (int i = 0; i < count; i++)
        {
            GameObject cardObject = activeObjects[i];
            if (cardObject == null)
                continue;

            CardUI ui = cardObject.GetComponent<CardUI>();

            if (ui == null)
                continue;

            if (ui.IsDragging || ui.isAnimating)
                continue;
        

            if (ui.HoverProgress > hoveredProgress)
            {
                hoveredProgress = ui.HoverProgress;
                hoveredIndex = i;
            }
        }

        float countT = (count <= 1)
            ? 0f
            : Mathf.InverseLerp(1f, cardsForMaxAngle, count);

        float targetTotalFanAngle =
            Mathf.Lerp(minTotalFanAngle, maxTotalFanAngle, countT) +
            hoveredProgress * hoverFanBoost;

        float targetRadius =
            Mathf.Lerp(minRadius, maxRadius, countT) +
            hoveredProgress * hoverRadiusBoost;

        if (snap)
        {
            currentTotalFanAngle = targetTotalFanAngle;
            currentRadius = targetRadius;
        }
        else
        {
            float t = Time.deltaTime * layoutSmoothSpeed;
            currentTotalFanAngle = Mathf.Lerp(currentTotalFanAngle, targetTotalFanAngle, t);
            currentRadius = Mathf.Lerp(currentRadius, targetRadius, t);
        }

        float startAngle = -currentTotalFanAngle * 0.5f;
        float endAngle = currentTotalFanAngle * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GameObject cardObject = activeObjects[i];
            if (cardObject == null)
                continue;

            RectTransform rect = cardObject.GetComponent<RectTransform>();
            CardUI ui = cardObject.GetComponent<CardUI>();

            if (ui != null && (ui.IsDragging || ui.isAnimating))
                continue;

            float t = (count == 1) ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            float radians = angle * Mathf.Deg2Rad;

            float x = Mathf.Sin(radians) * currentRadius;
            float y = Mathf.Cos(radians) * currentRadius - currentRadius;

            float hover = ui != null ? ui.HoverProgress : 0f;

            Vector2 targetPos = new Vector2(
                x,
                y + (hover * hoverLift)
            );

            Quaternion targetRot = Quaternion.Euler(0f, 0f, -angle);

            Vector3 targetScale = Vector3.one * (1f + hover * hoverScaleBoost);

            if (ui != null)
            {
                // Always set the target values — CardUI will perform the smoothing and actually write the RectTransform.
                ui.TargetPosition = targetPos;
                ui.TargetRotation = targetRot;
                ui.TargetScale = targetScale;

                // If snap is requested, also immediately apply them so the card doesn't interpolate visually.
                if (snap)
                {
                    RectTransform r = cardObject.GetComponent<RectTransform>();
                    if (r != null)
                    {
                        r.anchoredPosition = targetPos;
                        r.localRotation = targetRot;
                        r.localScale = targetScale;
                    }
                }
            }
            else
            {
                // If there's no CardUI for some reason, fall back to directly updating the rect.
                float s = Time.deltaTime * layoutSmoothSpeed;
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, s);
                rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRot, s);
                rect.localScale = Vector3.Lerp(rect.localScale, targetScale, s);
            }

            rect.SetSiblingIndex(i);
        }
        

        if (hoveredIndex >= 0 && hoveredIndex < activeObjects.Count)
        {
            GameObject hoveredObj = activeObjects[hoveredIndex];
            if (hoveredObj != null)
                hoveredObj.transform.SetAsLastSibling();
        }
    }
}