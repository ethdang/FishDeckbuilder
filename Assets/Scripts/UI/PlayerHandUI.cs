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

    [Header("Reveal / Draw Visuals")]
    [SerializeField] private float revealStagger = 0.12f; // time between sequential reveals
    [SerializeField] private float revealHold = 0.35f; // how long to hold at play target
    [SerializeField] private float revealFlipDuration = 0.16f; // flip time
    [SerializeField] private float discardSpeed = 0.05f; // speed of discard
    [SerializeField] private float drawAfterDiscardBuffer = 0.5f; // time after the discard hand before it starts drawing again
    [SerializeField] private float revealTimeScale = 1f;
    [SerializeField] private float revealMoveDuration = 0.18f;
    [SerializeField] private float revealEnlargeScale = 1.25f;

    private Queue<CardData> revealQueue = new Queue<CardData>();
    public bool isRevealing = false;

    private PlayerHand playerHand;
    private PlayerDeck playerDeck;

    public bool isDiscarding;
    private float currentTotalFanAngle;
    private float currentRadius;

    void Awake()
    {
        playerHand = FindFirstObjectByType<PlayerHand>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
    }

    public void AddCard(CardData card)
    {
        if (cardPrefab == null || objectParent == null)
        {
            Debug.LogError("PlayerHandUI.AddCard: cardPrefab or objectParent not assigned.");
            return;
        }

        GameObject obj = Instantiate(cardPrefab, objectParent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null && spawnPosition != null)
            rect.anchoredPosition = spawnPosition.anchoredPosition;

        CardUI ui = obj.GetComponent<CardUI>();
        if (ui != null)
            ui.Initialize(card, playerHand);

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

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
            return;

        bool removed = activeObjects.Remove(cardObject);
        if (!removed)
            Debug.LogWarning("PlayerHandUI.RemoveCardObject: object not found in activeObjects.", cardObject);
        else
            Destroy(cardObject);

        LayoutCards(false);
    }

    void LateUpdate()
    {
        if (isDiscarding)
            return;

        LayoutCards(false);
    }

    // inside PlayerHandUI.cs - Replace the body of DiscardCardsAnimated() with this:

    public IEnumerator DiscardCardsAnimated()
    {
        isDiscarding = true;

        Canvas rootCanvas = objectParent != null ? objectParent.GetComponentInParent<Canvas>() : null;
        RectTransform animationParent = rootCanvas != null ? (rootCanvas.transform as RectTransform) : objectParent;

        bool lastAnimationWasAnimated = false;
        bool lastAnimationCompleted = false;

        while (activeObjects.Count > 0)
        {
            GameObject obj = activeObjects[0];
            activeObjects.RemoveAt(0);

            if (obj == null)
            {
                yield return null;
                continue;
            }

            CardUI ui = obj.GetComponent<CardUI>();
            if (ui != null)
                ui.isAnimating = true;

            var anim = obj.GetComponent<CardActionAnimation>();
            CardData cardData = ui != null ? ui.Card : null;
            GameObject objToDestroy = obj;

            bool isLast = (activeObjects.Count == 0); // after removing the current one, activeObjects.Count==0 means this was the last

            if (anim != null)
            {
                // If this is the last started animation, arrange to be notified when it finishes.
                if (isLast)
                {
                    lastAnimationWasAnimated = true;
                    lastAnimationCompleted = false;

                    // Start the animation and in the callback set lastAnimationCompleted = true
                    StartCoroutine(anim.AnimateTo(animationParent, discardPile, () =>
                    {
                        if (cardData != null && playerDeck == null)
                            playerDeck = FindFirstObjectByType<PlayerDeck>();

                        if (cardData != null && playerDeck != null)
                            playerDeck.AddToDiscard(cardData);

                        Destroy(objToDestroy);

                        lastAnimationCompleted = true;
                    }));
                }
                else
                {
                    // Fire-and-forget non-last animations (they'll still add to discard via callback)
                    StartCoroutine(anim.AnimateTo(animationParent, discardPile, () =>
                    {
                        if (cardData != null && playerDeck == null)
                            playerDeck = FindFirstObjectByType<PlayerDeck>();

                        if (cardData != null && playerDeck != null)
                            playerDeck.AddToDiscard(cardData);

                        Destroy(objToDestroy);
                    }));
                }
            }
            else
            {
                // If no animation component, immediately handle discard data and destroy object.
                if (cardData != null)
                {
                    if (playerDeck == null)
                        playerDeck = FindFirstObjectByType<PlayerDeck>();

                    if (playerDeck != null)
                        playerDeck.AddToDiscard(cardData);
                }

                Debug.LogWarning("DiscardCardsAnimated: CardActionAnimation missing on card object.", objToDestroy);
                Destroy(objToDestroy);

                // If this was the last and there was no animation, ensure we don't wait for an animation that will never come.
                if (isLast)
                {
                    lastAnimationWasAnimated = false;
                    lastAnimationCompleted = true;
                }
            }

            // Stagger starts
            yield return new WaitForSeconds(discardSpeed);
        }

        // Wait for last animation (if it was an animated one)
        if (lastAnimationWasAnimated)
        {
            // Wait until the callback has flipped the flag
            yield return new WaitUntil(() => lastAnimationCompleted);
        }

        // Buffer before draws begin (unchanged)
        yield return new WaitForSeconds(drawAfterDiscardBuffer);

        playerHand.DiscardHand();
        isDiscarding = false;
    }

    // Enqueue a drawn card for sequential reveal. Call this from PlayerHand.DrawCard.
    public void EnqueueReveal(CardData card)
    {
        if (card == null) return;
        revealQueue.Enqueue(card);
        if (!isRevealing)
            StartCoroutine(ProcessRevealQueue());
    }

    private IEnumerator ProcessRevealQueue()
    {
        isRevealing = true;

        Canvas rootCanvas = objectParent != null ? objectParent.GetComponentInParent<Canvas>() : null;
        RectTransform animationParent = rootCanvas != null ? (rootCanvas.transform as RectTransform) : objectParent;

        Vector3 deckWorldPos = Vector3.zero;

        while (revealQueue.Count > 0)
        {
            CardData card = revealQueue.Dequeue();
            if (card == null) continue;

            if (playerDeck == null)
                playerDeck = FindFirstObjectByType<PlayerDeck>();

            if (playerDeck != null)
                deckWorldPos = playerDeck.transform.position;
            else if (objectParent != null)
                deckWorldPos = objectParent.position;

            yield return StartCoroutine(RevealFromDeckWorld(card, deckWorldPos));

            if (revealStagger > 0f)
                yield return new WaitForSeconds(revealStagger);
        }

        isRevealing = false;
    }

    public IEnumerator RevealFromDeckWorld(CardData card, Vector3 deckWorldPos)
    {
        if (cardPrefab == null || objectParent == null)
        {
            Debug.LogWarning("RevealFromDeckWorld: missing prefab or objectParent. Falling back to AddCard.");
            AddCard(card);
            yield break;
        }

        Canvas rootCanvas = objectParent != null ? objectParent.GetComponentInParent<Canvas>() : null;
        RectTransform animationParent = rootCanvas != null ? (rootCanvas.transform as RectTransform) : objectParent;
        Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;

        // Instantiate visual under animationParent (not part of the hand yet)
        GameObject obj = Instantiate(cardPrefab);
        obj.transform.SetParent(animationParent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        CardUI ui = obj.GetComponent<CardUI>();
        if (ui != null)
            ui.Initialize(card, playerHand);

        // Ensure CanvasGroup for fading
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Determine play-zone center local position (fallback to deckWorldPos)
        Vector2 playLocal;
        PlayZone playZone = FindFirstObjectByType<PlayZone>();
        RectTransform playTarget = (playZone != null && playZone.transform.childCount > 0)
            ? playZone.transform.GetChild(0).GetComponent<RectTransform>()
            : null;

        if (playTarget != null)
        {
            Vector2 playScreen = RectTransformUtility.WorldToScreenPoint(cam, playTarget.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, playScreen, cam, out playLocal);
        }
        else
        {
            Vector2 deckScreen = RectTransformUtility.WorldToScreenPoint(cam, deckWorldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, deckScreen, cam, out playLocal);
        }

        rect.anchoredPosition = playLocal;
        rect.localScale = Vector3.one;

        // Timing / scale parameters (explicit fields, no reflection)
        float rt = Mathf.Max(0.0001f, revealTimeScale);                         // global time scale
        float presentDur = Mathf.Max(0.01f, revealFlipDuration * rt);           // duration for combined fade+scale+flip
        float holdDur = Mathf.Max(0f, revealHold * rt);                         // hold AFTER present
        float moveDur = Mathf.Max(0.01f, revealMoveDuration * rt);             // move duration (use revealMoveDuration field)
        float enlargeScale = Mathf.Max(1f, revealEnlargeScale);                // scale used during present

        // Mark animating so LayoutCards ignores it
        if (ui != null) ui.isAnimating = true;

        float fastAlphaRamp = 0.33f; // fraction of presentDur used to ramp alpha -> 1 quickly

        float timer = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * enlargeScale;

        while (timer < presentDur)
        {
            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / presentDur);
            float smooth = Mathf.SmoothStep(0f, 1f, p);
            
            float alphaT = Mathf.Clamp01(p / fastAlphaRamp); // 0->1 in first (fastAlphaRamp) fraction
            cg.alpha = Mathf.Lerp(0f, 1f, alphaT);

            // Uniform scaling 1 -> enlargeScale
            Vector3 uniformScale = Vector3.Lerp(startScale, targetScale, smooth);

            // Reverse flip: angle goes 180 -> 360
            float angle = Mathf.Lerp(180f, 360f, smooth);
            float width = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));

            // Apply combined transform: X scaled by flip width, Y/Z by uniform scale
            rect.localScale = new Vector3(width * uniformScale.x, uniformScale.y, uniformScale.z);

            yield return null;
        }

        // ensure final presented state
        cg.alpha = 1f;
        rect.localScale = Vector3.one * enlargeScale;

        // HOLD after the present (this is the added hold you requested)
        if (holdDur > 0f)
            yield return new WaitForSeconds(holdDur);

        // MOVE: compute hand spawn local position under animationParent
        Vector2 handSpawnLocal;
        if (spawnPosition != null)
        {
            Vector2 spawnScreen = RectTransformUtility.WorldToScreenPoint(cam, spawnPosition.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, spawnScreen, cam, out handSpawnLocal);
        }
        else if (objectParent != null)
        {
            handSpawnLocal = objectParent.anchoredPosition;
        }
        else
        {
            handSpawnLocal = Vector2.zero;
        }

        // Manual LERP move to hand while scaling back to 1
        Vector2 from = rect.anchoredPosition;
        float m = 0f;
        Vector3 fromScale = rect.localScale;
        while (m < moveDur)
        {
            m += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, m / moveDur);
            rect.anchoredPosition = Vector2.Lerp(from, handSpawnLocal, t);
            rect.localScale = Vector3.Lerp(fromScale, Vector3.one, t);
            yield return null;
        }
        rect.anchoredPosition = handSpawnLocal;
        rect.localScale = Vector3.one;
        cg.alpha = 1f;

        // RE-PARENT into hand and finalize
        obj.transform.SetParent(objectParent, false);

        if (rect != null)
        {
            rect.anchoredPosition = spawnPosition != null ? spawnPosition.anchoredPosition : Vector2.zero;
            rect.localScale = Vector3.one;
        }

        if (ui != null)
        {
            var refresh = ui.GetType().GetMethod("RefreshHandParentRect");
            if (refresh != null)
                refresh.Invoke(ui, new object[] { objectParent });
            ui.isAnimating = false;
        }

        cg.interactable = true;
        cg.blocksRaycasts = true;
        cg.alpha = 1f;

        activeObjects.Add(obj);
        LayoutCards(false);

        yield break;
    }

    // Helper to create a temporary RectTransform under parent at the world position converted to parent's local space
    private RectTransform CreateTempAnchorForWorldPoint(RectTransform parent, Vector3 worldPos)
    {
        GameObject go = new GameObject("TempAnchor_return", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera cam = canvas != null ? canvas.worldCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, cam, out local);
        rt.anchoredPosition = local;
        rt.sizeDelta = Vector2.zero;
        return rt;
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
                if (snap)
                {
                    ui.TargetPosition = targetPos;
                    ui.TargetRotation = targetRot;
                    ui.TargetScale = targetScale;
                }
                else
                {
                    float s = Time.deltaTime * layoutSmoothSpeed;
                    rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, s);
                    rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRot, s);
                    rect.localScale = Vector3.Lerp(rect.localScale, targetScale, s);
                }
            }

            rect.SetSiblingIndex(i);
        }

        if (hoveredIndex >= 0 && hoveredIndex < activeObjects.Count)
        {
            activeObjects[hoveredIndex].transform.SetAsLastSibling();
        }
    }
}