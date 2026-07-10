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

    [Header("Reveal / Draw Visuals")]
    [SerializeField] private float revealStagger = 0.12f; // time between sequential reveals
    [SerializeField] private float revealHold = 0.35f; // base hold at play target (will be scaled by revealTimeScale)
    [SerializeField] private float revealFlipDuration = 0.16f; // base flip time (scaled by revealTimeScale)

    [Header("Control & Buffer")]
    [Tooltip("Time to wait AFTER discard animations finish and BEFORE drawing starts.")]
    [SerializeField] public float postDiscardBuffer = 0.08f;

    [Tooltip("Global time scale applied to reveal animations: 1 = normal, 0.5 = twice as fast, 2 = half speed.")]
    [SerializeField] private float revealTimeScale = 1f;

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

    // Discard: wait sequentially for each card's animation to finish (deterministic) and add to discard on completion.
    public IEnumerator DiscardCardsAnimated()
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
                // apply global revealTimeScale to discard animations as well so you can tune speed in one place
                anim.timeScale = revealTimeScale;

                CardData cardData = ui != null ? ui.Card : null;
                GameObject objToDestroy = obj;

                // Wait for the animation to complete (sequential)
                yield return StartCoroutine(anim.AnimateTo(animationParent, discardPile, () =>
                {
                    if (cardData != null && playerDeck == null)
                        playerDeck = FindFirstObjectByType<PlayerDeck>();

                    if (cardData != null && playerDeck != null)
                        playerDeck.AddToDiscard(cardData);

                    Destroy(objToDestroy);
                }));
            }
            else
            {
                Debug.LogWarning("DiscardCardsAnimated: CardActionAnimation missing on card object.", obj);
                Destroy(obj);
            }

            yield return new WaitForSeconds(0.05f);
        }

        // Ensure logical hand is cleared (defensive)
        if (playerHand != null)
            playerHand.DiscardAllInstant();

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
        if (isRevealing) yield break;
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

            // Run the reveal (the reveal method will set anim.timeScale and scale local waits)
            yield return StartCoroutine(RevealFromDeckWorld(card, deckWorldPos));

            if (revealStagger > 0f)
                yield return new WaitForSeconds(revealStagger);
        }

        isRevealing = false;
    }

    // Reveal-from-deck flow:
    // Instantiate a visual at deckWorldPos (not parented under the hand), animate it to the play target,
    // present it, then immediately reparent it into the hand (no return animation), and register it in activeObjects.
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

        GameObject obj = Instantiate(cardPrefab);
        obj.name = $"RevealTemp_{card.cardName}_{obj.GetInstanceID()}";
        obj.transform.SetParent(animationParent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        CardUI ui = obj.GetComponent<CardUI>();
        if (ui != null)
            ui.Initialize(card, playerHand);

        // place at deck/spawn local pos (animationParent local space)
        Vector2 deckScreen = RectTransformUtility.WorldToScreenPoint(cam, deckWorldPos);
        Vector2 deckLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, deckScreen, cam, out deckLocal);
        rect.anchoredPosition = deckLocal;
        rect.localScale = Vector3.one;

        // Find play target (center)
        PlayZone playZone = FindFirstObjectByType<PlayZone>();
        RectTransform playTarget = null;
        if (playZone != null && playZone.transform.childCount > 0)
            playTarget = playZone.transform.GetChild(0).GetComponent<RectTransform>();

        // If CardActionAnimation is available, set its timeScale to revealTimeScale so it uses scaled durations
        var anim = obj.GetComponent<CardActionAnimation>();
        if (anim != null)
            anim.timeScale = revealTimeScale;

        // Move to play target using CardActionAnimation if available (it will use its own durations scaled by timeScale).
        if (anim != null)
        {
            var noFade = anim.GetType().GetMethod("AnimateToNoFade");
            if (noFade != null)
                yield return StartCoroutine(anim.AnimateToNoFade(animationParent, playTarget != null ? playTarget : animationParent, null));
            else
                yield return StartCoroutine(anim.AnimateTo(animationParent, playTarget != null ? playTarget : animationParent, null));
        }
        else
        {
            // fallback manual move (respect revealTimeScale on wait lengths)
            Vector2 start = rect.anchoredPosition;
            Vector2 target = Vector2.zero;
            if (playTarget != null)
            {
                Vector2 playScreen = RectTransformUtility.WorldToScreenPoint(cam, playTarget.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(animationParent, playScreen, cam, out target);
            }
            float dur = 0.18f * revealTimeScale;
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

        // Present / flip effect -- scale durations by revealTimeScale
        float flipDur = revealFlipDuration * revealTimeScale;
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

        // Hold so the player sees the revealed card (scaled)
        yield return new WaitForSeconds(revealHold * revealTimeScale);

        // Immediately reparent into the hand
        obj.transform.SetParent(objectParent, false);

        if (rect != null)
        {
            rect.anchoredPosition = spawnPosition != null ? spawnPosition.anchoredPosition : Vector2.zero;
            rect.localScale = Vector3.one;
        }

        // Notify CardUI of new parent rect if method exists
        if (ui != null)
        {
            var refresh = ui.GetType().GetMethod("RefreshHandParentRect");
            if (refresh != null)
                refresh.Invoke(ui, new object[] { objectParent });
            ui.isAnimating = false;
        }

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;

        activeObjects.Add(obj);
        LayoutCards(false);

        yield break;
    }

    // Layout code unchanged...
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