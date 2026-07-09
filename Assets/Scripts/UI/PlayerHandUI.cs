using System;
using System.Collections;
using System.Collections.Generic;
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

    // Reveal queue settings
    [Header("Reveal / Draw Visuals")]
    [SerializeField] private float revealStagger = 0.12f; // time between sequential reveals
    [SerializeField] private float revealHold = 0.35f; // how long to hold at play target
    [SerializeField] private float revealFlipDuration = 0.16f; // flip time

    private Queue<CardData> revealQueue = new Queue<CardData>();
    private bool isRevealing = false;

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

    // --- Visual management (Add / Remove) ---

    public void AddCard(CardData card)
    {
        if (cardPrefab == null || objectParent == null)
        {
            Debug.LogError("PlayerHandUI.AddCard: cardPrefab or objectParent not assigned.");
            return;
        }

        // Instantiate without preserving world position so local anchoredPosition is meaningful
        GameObject obj = Instantiate(cardPrefab);
        obj.transform.SetParent(objectParent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = spawnPosition != null ? spawnPosition.anchoredPosition : Vector2.zero;

        CardUI ui = obj.GetComponent<CardUI>();
        if (ui != null)
            ui.Initialize(card, playerHand);

        // Ensure CanvasGroup settings are interactive
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

    // --- Discard animation (staggered, per-card callback) ---
    public IEnumerator DiscardCardsAnimated(float stagger = 0.08f, bool addToDiscardOnFinish = true)
    {
        isDiscarding = true;

        Canvas rootCanvas = objectParent != null ? objectParent.GetComponentInParent<Canvas>() : null;
        RectTransform animationParent = rootCanvas != null ? (rootCanvas.transform as RectTransform) : objectParent;

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
            if (anim != null)
            {
                CardData cardData = ui != null ? ui.Card : null;
                GameObject objToDestroy = obj;

                // Start animation without waiting (so animations overlap per stagger)
                StartCoroutine(anim.AnimateTo(animationParent, discardPile, () =>
                {
                    // Called when this card's animation completes
                    if (addToDiscardOnFinish && cardData != null)
                    {
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

            yield return new WaitForSeconds(stagger);
        }

        isDiscarding = false;
    }

    // --- Reveal / draw queue API ---

    // Called by PlayerHand.DrawCard (or similar) to show the draw animation in sequence
    public void EnqueueReveal(CardData card)
    {
        if (card == null) return;
        revealQueue.Enqueue(card);
        if (!isRevealing)
            StartCoroutine(ProcessRevealQueue());
    }

    public IEnumerator RevealFromDeckWorld(CardData card, Vector3 deckWorldPos)
    {
        if (cardPrefab == null || objectParent == null)
        {
            Debug.LogWarning("RevealFromDeckWorld: missing prefab or objectParent. Falling back to AddCard.");
            AddCard(card);
            yield break;
        }

        // Choose animation parent: top-most canvas rect so screen-space math is stable
        Canvas rootCanvas = objectParent != null ? objectParent.GetComponentInParent<Canvas>() : null;
        RectTransform animationParent = rootCanvas != null ? (rootCanvas.transform as RectTransform) : objectParent;

        // Instantiate the visual under animationParent (not in the hand)
        GameObject obj = Instantiate(cardPrefab);
        obj.name = $"RevealTemp_{card.cardName}_{obj.GetInstanceID()}";
        obj.transform.SetParent(animationParent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        CardUI ui = obj.GetComponent<CardUI>();
        if (ui != null)
            ui.Initialize(card, playerHand); // sets texts etc.

        // Place the temp visual at the deck world position (convert into animationParent's local coords)
        Camera cam = rootCanvas != null ? rootCanvas.worldCamera : null;
        Vector2 deckScreen = RectTransformUtility.WorldToScreenPoint(cam, deckWorldPos);
        Vector2 deckLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, deckScreen, cam, out deckLocal);
        rect.anchoredPosition = deckLocal;

        // Find the play target (use PlayZone's first child like your play animation does)
        PlayZone playZone = FindFirstObjectByType<PlayZone>();
        RectTransform playTarget = null;
        if (playZone != null && playZone.transform.childCount > 0)
            playTarget = playZone.transform.GetChild(0).GetComponent<RectTransform>();

        // Use CardActionAnimation.AnimateToNoFade if present; fallback to manual move
        CardActionAnimation anim = obj.GetComponent<CardActionAnimation>();
        if (anim != null)
        {
            // Use AnimateToNoFade if available (preferred)
            var method = anim.GetType().GetMethod("AnimateToNoFade");
            if (method != null)
            {
                yield return StartCoroutine(anim.AnimateToNoFade(animationParent, playTarget != null ? playTarget : animationParent, null));
            }
            else
            {
                // fallback to AnimateTo (may do fade). We still use it because it's available.
                yield return StartCoroutine(anim.AnimateTo(animationParent, playTarget != null ? playTarget : animationParent, null));
            }
        }
        else
        {
            // manual move: lerp from current to center of animationParent (0,0) or to playTarget if present
            Vector2 start = rect.anchoredPosition;
            Vector2 target = Vector2.zero;
            if (playTarget != null)
            {
                Vector2 playScreen = RectTransformUtility.WorldToScreenPoint(cam, playTarget.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, playScreen, cam, out target);
            }
            float dur = 0.18f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, t / dur);
                rect.anchoredPosition = Vector2.Lerp(start, target, s);
                yield return null;
            }
            rect.anchoredPosition = target;
        }

        // "Present" / flip effect (scale X flip)
        float flipDur = 0.16f;
        float elapsed = 0f;
        float flipScale = 1.25f;
        while (elapsed < flipDur)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / flipDur;
            float angle = Mathf.Lerp(0f, 180f, p);
            float width = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));
            rect.localScale = new Vector3(width * flipScale, rect.localScale.y, rect.localScale.z);
            yield return null;
        }
        rect.localScale = Vector3.one * flipScale;

        // Hold so the player sees the revealed card briefly
        yield return new WaitForSeconds(0.35f);

        // Compute the spawn target (where it should return into the hand), in animationParent local coords
        Vector2 spawnLocal = Vector2.zero;
        if (spawnPosition != null)
        {
            Vector2 spawnScreen = RectTransformUtility.WorldToScreenPoint(cam, spawnPosition.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, spawnScreen, cam, out spawnLocal);
        }
        else
        {
            // if spawnPosition missing, just place slightly below center
            spawnLocal = new Vector2(0f, -100f);
        }

        // Animate back to the spawnLocal
        if (anim != null)
        {
            // create a temporary RectTransform under animationParent to use as a target
            GameObject tmpReturn = new GameObject("TempReturnTarget", typeof(RectTransform));
            tmpReturn.transform.SetParent(animationParent, false);
            RectTransform tmpRT = tmpReturn.GetComponent<RectTransform>();
            tmpRT.anchoredPosition = spawnLocal;
            tmpRT.sizeDelta = Vector2.zero;

            var method = anim.GetType().GetMethod("AnimateToNoFade");
            if (method != null)
                yield return StartCoroutine(anim.AnimateToNoFade(animationParent, tmpRT, null));
            else
                yield return StartCoroutine(anim.AnimateTo(animationParent, tmpRT, null));

            Destroy(tmpReturn);
        }
        else
        {
            Vector2 start2 = rect.anchoredPosition;
            float d2 = 0.18f;
            float tt = 0f;
            while (tt < d2)
            {
                tt += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, tt / d2);
                rect.anchoredPosition = Vector2.Lerp(start2, spawnLocal, s);
                yield return null;
            }
            rect.anchoredPosition = spawnLocal;
        }

        // Now reparent the same object into the hand (objectParent). Use worldPositionStays = false so we can set anchoredPosition properly.
        obj.transform.SetParent(objectParent, false);

        // set anchored position to spawnPosition's anchored pos so hand layout starts from a sensible spot
        if (rect != null)
        {
            rect.anchoredPosition = spawnPosition != null ? spawnPosition.anchoredPosition : Vector2.zero;
            rect.localScale = Vector3.one; // normalize scale
        }

        // Ensure the CardUI knows the new hand parent for drag math
        if (ui != null)
        {
            ui.RefreshHandParentRect(objectParent);
            // also ensure the card is not flagged as animating so LayoutCards can control it
            ui.isAnimating = false;
        }

        // Restore or add CanvasGroup interactivity so the card is clickable
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        // Register the canonical card with the hand visuals and re-layout
        activeObjects.Add(obj);
        LayoutCards(false);

        yield break;
    }

    private IEnumerator ProcessRevealQueue()
    {
        isRevealing = true;

        Canvas rootCanvas = objectParent != null ? objectParent.GetComponentInParent<Canvas>() : null;
        RectTransform animationParent = rootCanvas != null ? (rootCanvas.transform as RectTransform) : objectParent;

        // find play area target if present (the same target used for playing cards)
        PlayZone playZone = FindFirstObjectByType<PlayZone>();
        RectTransform playTarget = null;
        if (playZone != null && playZone.transform.childCount > 0)
            playTarget = playZone.transform.GetChild(0).GetComponent<RectTransform>();

        while (revealQueue.Count > 0)
        {
            CardData card = revealQueue.Dequeue();
            if (card == null) continue;

            yield return StartCoroutine(RevealToPlayAreaAndBack(card, animationParent, playTarget));

            if (revealStagger > 0f)
                yield return new WaitForSeconds(revealStagger);
        }

        isRevealing = false;
    }

    // Instantiate a temporary reveal visual, animate it to the play target (or center),
    // flip/present it, animate back to spawn, then create the canonical hand card (AddCard).
    private IEnumerator RevealToPlayAreaAndBack(CardData card, RectTransform animationParent, RectTransform playTarget)
    {
        Debug.Log($"[PlayerHandUI] Reveal start for '{card.cardName}'");

        // create temp
        GameObject temp = Instantiate(cardPrefab);
        temp.name = $"RevealTemp_{card.cardName}_{temp.GetInstanceID()}";
        temp.transform.SetParent(animationParent, false);
        RectTransform tempRect = temp.GetComponent<RectTransform>();
        CardUI tempUI = temp.GetComponent<CardUI>();
        if (tempUI != null) tempUI.Initialize(card, playerHand);

        CardActionAnimation anim = temp.GetComponent<CardActionAnimation>();
        if (anim == null)
        {
            Debug.LogWarning("[PlayerHandUI] RevealToPlayAreaAndBack: CardActionAnimation missing on prefab, skipping reveal animation.");
            AddCard(card);
            Destroy(temp);
            yield break;
        }

        // reveal target: prefer playTarget, else center anchor created under animationParent
        RectTransform centerTarget = playTarget;
        GameObject centerGO = null;
        if (centerTarget == null)
        {
            centerGO = new GameObject("TempRevealCenter", typeof(RectTransform));
            centerGO.transform.SetParent(animationParent, false);
            centerTarget = centerGO.GetComponent<RectTransform>();
            centerTarget.anchoredPosition = Vector2.zero;
            centerTarget.sizeDelta = Vector2.zero;
        }

        // Animate to play area (no fade)
        yield return StartCoroutine(anim.AnimateToNoFade(animationParent, centerTarget, null));

        // flip/present
        float fDur = revealFlipDuration;
        float elapsed = 0f;
        float flipScale = 1.25f;
        while (elapsed < fDur)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / fDur;
            float ang = Mathf.Lerp(0f, 180f, p);
            float w = Mathf.Abs(Mathf.Cos(ang * Mathf.Deg2Rad));
            tempRect.localScale = new Vector3(w * flipScale, tempRect.localScale.y, 1f);
            yield return null;
        }
        tempRect.localScale = Vector3.one * flipScale;

        // hold
        yield return new WaitForSeconds(revealHold);

        // compute return anchor under animationParent using spawnPosition world point
        RectTransform returnTarget = CreateTempAnchorForWorldPoint(animationParent, spawnPosition != null ? spawnPosition.position : (objectParent != null ? objectParent.position : Vector3.zero));

        // animate back to hand
        yield return StartCoroutine(anim.AnimateToNoFade(animationParent, returnTarget, null));

        // cleanup
        if (centerGO != null) Destroy(centerGO);
        if (returnTarget != null && returnTarget.gameObject.name.StartsWith("TempAnchor_return")) Destroy(returnTarget.gameObject);
        Destroy(temp);

        // add canonical card into hand (interactive)
        AddCard(card);

        Debug.Log($"[PlayerHandUI] Reveal complete for '{card.cardName}'");
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

    // --- Layout ---

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
                // Always set target values and let CardUI perform smoothing to avoid write races
                ui.TargetPosition = targetPos;
                ui.TargetRotation = targetRot;
                ui.TargetScale = targetScale;

                if (snap)
                {
                    if (rect != null)
                    {
                        rect.anchoredPosition = targetPos;
                        rect.localRotation = targetRot;
                        rect.localScale = targetScale;
                    }
                }
            }
            else
            {
                // Fallback if no CardUI present
                float s = Time.deltaTime * layoutSmoothSpeed;
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPos, s);
                    rect.localRotation = Quaternion.Slerp(rect.localRotation, targetRot, s);
                    rect.localScale = Vector3.Lerp(rect.localScale, targetScale, s);
                }
            }

            if (rect != null)
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