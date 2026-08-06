using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class EffectPanelUI : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float fadeLength = 0.4f;
    [SerializeField] private float moveDuration = 0.25f;    // how long position moves take
    [SerializeField] private float staggerDelay = 0.06f;    // delay between each item's start

    [Header("Active Objects")]
    public List<GameObject> activeCurrentObjs = new();
    public List<GameObject> activeUpcomingObjs = new();

    [Header("Prefab")]
    [SerializeField] private GameObject currentEffectPrefab;
    [SerializeField] private GameObject upcomingEffectPrefab;

    [Header("Parent")]
    [SerializeField] private RectTransform currentEffectParent;
    [SerializeField] private RectTransform upcomingEffectParent;

    private TurnManager turnManager;
    private CardManager cardManager;
    private Coroutine updateRoutine;
    private Coroutine currentUpdateRoutine;
    private readonly Dictionary<GameObject, Coroutine> moveCoroutines = new();
    private readonly Dictionary<GameObject, Coroutine> fadeCoroutines = new();

    void Awake()
    {
        turnManager = FindFirstObjectByType<TurnManager>();
        cardManager = FindFirstObjectByType<CardManager>();
    }

    public void UpdateUpcomingEffects()
    {
        if (updateRoutine != null)
            StopCoroutine(updateRoutine);

        updateRoutine = StartCoroutine(UpcomingEffectsRoutine());
    }

    public void UpdateCurrentEffects()
    {
        if (currentUpdateRoutine != null)
            StopCoroutine(currentUpdateRoutine);

        currentUpdateRoutine = StartCoroutine(CurrentEffectsRoutine());
    }

    public IEnumerator UpcomingEffectsRoutine()
    {
        if (turnManager == null || turnManager.pendingEffects == null)
            yield break;

        int targetCount = turnManager.pendingEffects.Count;
        float spacing = 10f;

        if (upcomingEffectParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(upcomingEffectParent);
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        // Build a fresh ordered list so the UI always reflects the current pending-effects order.
        List<GameObject> nextUpcomingObjs = new(targetCount);
        bool collapseToSingle = targetCount == 1 && activeUpcomingObjs.Count > 1;

        for (int i = 0; i < targetCount; i++)
        {
            PendingEffect effect = turnManager.pendingEffects[i];

            GameObject item = GetOrCreateUpcomingItem(i, collapseToSingle);
            if (item == null) continue;

            nextUpcomingObjs.Add(item);

            CanvasGroup cg = EnsureCanvasGroup(item);
            if (cg != null && !IsReusedItem(item, i, collapseToSingle))
                cg.alpha = 0f;

            RectTransform rt = item.GetComponent<RectTransform>();
            Vector2 targetAnchored = GetTargetAnchored(rt, i, spacing);
            ApplyTargetPosition(rt, targetAnchored, IsReusedItem(item, i, collapseToSingle));
            UpdateEffectText(item, effect);

            float delay = i * staggerDelay;
            if (IsReusedItem(item, i, collapseToSingle))
                StartMoveCoroutine(item, rt, targetAnchored, delay, collapseToSingle && i == 0);

            StartFadeCoroutine(item, cg, delay, collapseToSingle && i == 0);
        }

        // Fade out & destroy any items that are no longer in the current pending-effects list.
        HashSet<GameObject> nextSet = new(nextUpcomingObjs);
        List<GameObject> staleItems = new();
        foreach (GameObject old in activeUpcomingObjs)
        {
            if (old == null) continue;
            if (!nextSet.Contains(old))
                staleItems.Add(old);
        }

        for (int i = 0; i < staleItems.Count; i++)
        {
            GameObject old = staleItems[i];
            if (old == null) continue;
            CleanupCoroutines(old);

            CanvasGroup cgOld = old.GetComponent<CanvasGroup>();
            float delay = targetCount * staggerDelay + i * staggerDelay;
            if (cgOld != null)
                StartCoroutine(FadeTo(cgOld, 0f, delay));
            StartCoroutine(DestroyAfter(old, delay + fadeLength));
        }

        activeUpcomingObjs.Clear();
        activeUpcomingObjs.AddRange(nextUpcomingObjs);

        // wait for the whole stagger + transition to complete
        float totalWait = Mathf.Max(moveDuration, fadeLength) + Mathf.Max(0f, (targetCount - 1) * staggerDelay);
        yield return new WaitForSeconds(totalWait);
    }

    public IEnumerator CurrentEffectsRoutine()
    {
        if (cardManager == null || cardManager.modifiers == null)
            yield break;

        int targetCount = cardManager.modifiers.Count;
        float spacing = 10f;

        if (currentEffectParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentEffectParent);
            Canvas.ForceUpdateCanvases();
            yield return null;
        }

        List<GameObject> nextCurrentObjs = new(targetCount);
        bool collapseToSingle = targetCount == 1 && activeCurrentObjs.Count > 1;

        for (int i = 0; i < targetCount; i++)
        {
            CardModifier modifier = cardManager.modifiers[i];
            GameObject item = GetOrCreateCurrentItem(i, collapseToSingle);
            if (item == null) continue;

            nextCurrentObjs.Add(item);

            CanvasGroup cg = EnsureCanvasGroup(item);
            if (cg != null && !IsReusedCurrentItem(item, i, collapseToSingle))
                cg.alpha = 0f;

            RectTransform rt = item.GetComponent<RectTransform>();
            Vector2 targetAnchored = GetTargetAnchored(rt, i, spacing);
            ApplyTargetPosition(rt, targetAnchored, IsReusedCurrentItem(item, i, collapseToSingle));
            UpdateModifierText(item, modifier);

            float delay = i * staggerDelay;
            if (IsReusedCurrentItem(item, i, collapseToSingle))
                StartMoveCoroutine(item, rt, targetAnchored, delay, false);

            StartFadeCoroutine(item, cg, delay, false);
        }

        HashSet<GameObject> nextSet = new(nextCurrentObjs);
        List<GameObject> staleItems = new();
        foreach (GameObject old in activeCurrentObjs)
        {
            if (old == null) continue;
            if (!nextSet.Contains(old))
                staleItems.Add(old);
        }

        for (int i = 0; i < staleItems.Count; i++)
        {
            GameObject old = staleItems[i];
            if (old == null) continue;
            CleanupCoroutines(old);

            CanvasGroup cgOld = old.GetComponent<CanvasGroup>();
            float delay = targetCount * staggerDelay + i * staggerDelay;
            if (cgOld != null)
                StartCoroutine(FadeTo(cgOld, 0f, delay));
            StartCoroutine(DestroyAfter(old, delay + fadeLength));
        }

        activeCurrentObjs.Clear();
        activeCurrentObjs.AddRange(nextCurrentObjs);

        float totalWait = Mathf.Max(moveDuration, fadeLength) + Mathf.Max(0f, (targetCount - 1) * staggerDelay);
        yield return new WaitForSeconds(totalWait);
    }

    public IEnumerator FadeTo(CanvasGroup cg, float amount, float delay = 0f)
    {
        if (cg == null)
            yield break;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (cg == null)
            yield break;

        float timer = 0f;
        float startingAmt = cg.alpha;

        while (timer < fadeLength)
        {
            if (cg == null)
                yield break;

            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / fadeLength);
            cg.alpha = Mathf.Lerp(startingAmt, amount, p);
            yield return null;
        }

        if (cg != null)
            cg.alpha = amount;
    }

    private GameObject GetOrCreateCurrentItem(int index, bool collapseToSingle)
    {
        if (collapseToSingle)
        {
            int sourceIndex = activeCurrentObjs.Count - 1;
            if (sourceIndex >= 0 && activeCurrentObjs[sourceIndex] != null)
                return activeCurrentObjs[sourceIndex];
        }
        else if (index < activeCurrentObjs.Count && activeCurrentObjs[index] != null)
        {
            return activeCurrentObjs[index];
        }

        return Instantiate(currentEffectPrefab, currentEffectParent, false);
    }

    private bool IsReusedCurrentItem(GameObject item, int index, bool collapseToSingle)
    {
        if (collapseToSingle)
            return item != null && activeCurrentObjs.Count > 1 && activeCurrentObjs.Contains(item);

        return item != null && index < activeCurrentObjs.Count && activeCurrentObjs[index] == item;
    }

    private void UpdateModifierText(GameObject item, CardModifier modifier)
    {
        if (item == null || modifier == null)
            return;

        DelayText delayComp = item.GetComponentInChildren<DelayText>();
        if (delayComp != null)
        {
            TMP_Text delayText = delayComp.GetComponent<TMP_Text>();
            if (delayText != null) delayText.text = modifier.remainingUses.ToString();
        }

        EffectText effectComp = item.GetComponentInChildren<EffectText>();
        if (effectComp != null)
        {
            TMP_Text effectText = effectComp.GetComponent<TMP_Text>();
            if (effectText != null) effectText.text = modifier.ToString();
        }
        else
        {
            TMP_Text text = item.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = modifier.ToString();
        }
    }

    private GameObject GetOrCreateUpcomingItem(int index, bool collapseToSingle)
    {
        if (collapseToSingle)
        {
            int sourceIndex = activeUpcomingObjs.Count - 1;
            if (sourceIndex >= 0 && activeUpcomingObjs[sourceIndex] != null)
                return activeUpcomingObjs[sourceIndex];
        }
        else if (index < activeUpcomingObjs.Count && activeUpcomingObjs[index] != null)
        {
            return activeUpcomingObjs[index];
        }

        return Instantiate(upcomingEffectPrefab, upcomingEffectParent, false);
    }

    private CanvasGroup EnsureCanvasGroup(GameObject item)
    {
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = item.AddComponent<CanvasGroup>();
        return cg;
    }

    private bool IsReusedItem(GameObject item, int index, bool collapseToSingle)
    {
        if (collapseToSingle)
            return item != null && activeUpcomingObjs.Count > 1 && activeUpcomingObjs.Contains(item);

        return item != null && index < activeUpcomingObjs.Count && activeUpcomingObjs[index] == item;
    }

    private Vector2 GetTargetAnchored(RectTransform rt, int index, float spacing)
    {
        if (rt == null)
            return Vector2.zero;

        float height = rt.rect.height > 0f ? rt.rect.height : 40f;
        float width = rt.rect.width > 0f ? rt.rect.width : 40f;
        return new Vector2(width / 2f, -index * (height + spacing));
    }

    private void ApplyTargetPosition(RectTransform rt, Vector2 targetAnchored, bool reused)
    {
        if (rt == null)
            return;

        if (!reused)
        {
            rt.anchoredPosition = targetAnchored;
            return;
        }

        if (rt.anchoredPosition == targetAnchored)
            rt.anchoredPosition = targetAnchored;
    }

    private void UpdateEffectText(GameObject item, PendingEffect effect)
    {
        if (item == null || effect == null)
            return;

        DelayText delayComp = item.GetComponentInChildren<DelayText>();
        if (delayComp != null)
        {
            TMP_Text delayText = delayComp.GetComponent<TMP_Text>();
            if (delayText != null) delayText.text = effect.turnsRemaining.ToString();
        }

        EffectText effectComp = item.GetComponentInChildren<EffectText>();
        if (effectComp != null)
        {
            TMP_Text effectText = effectComp.GetComponent<TMP_Text>();
            if (effectText != null) effectText.text = effect.effect.ToString();
        }
    }

    private void StartMoveCoroutine(GameObject item, RectTransform rt, Vector2 targetAnchored, float delay, bool collapseToSingle)
    {
        if (item == null || rt == null)
            return;

        if (moveCoroutines.ContainsKey(item))
        {
            StopCoroutine(moveCoroutines[item]);
            moveCoroutines.Remove(item);
        }

        Vector2 from = rt.anchoredPosition;
        if (from == targetAnchored)
        {
            rt.anchoredPosition = targetAnchored;
            return;
        }

        float startDelay = collapseToSingle ? fadeLength : delay;
        moveCoroutines[item] = StartCoroutine(MoveRectTo(rt, targetAnchored, moveDuration, startDelay));
    }

    private void StartFadeCoroutine(GameObject item, CanvasGroup cg, float delay, bool collapseToSingle)
    {
        if (item == null || cg == null)
            return;

        if (fadeCoroutines.ContainsKey(item))
        {
            StopCoroutine(fadeCoroutines[item]);
            fadeCoroutines.Remove(item);
        }

        float fadeDelay = collapseToSingle ? fadeLength : delay;
        fadeCoroutines[item] = StartCoroutine(FadeTo(cg, 1f, fadeDelay));
    }

    private void CleanupCoroutines(GameObject obj)
    {
        if (obj == null)
            return;

        if (moveCoroutines.ContainsKey(obj))
        {
            StopCoroutine(moveCoroutines[obj]);
            moveCoroutines.Remove(obj);
        }

        if (fadeCoroutines.ContainsKey(obj))
        {
            StopCoroutine(fadeCoroutines[obj]);
            fadeCoroutines.Remove(obj);
        }
    }

    private IEnumerator MoveRectTo(RectTransform rt, Vector2 target, float duration, float delay = 0f)
    {
        if (rt == null)
            yield break;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    private IEnumerator MoveLocalTo(Transform tr, Vector3 targetLocal, float duration, float delay = 0f)
    {
        if (tr == null)
            yield break;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Vector3 start = tr.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);
            tr.localPosition = Vector3.LerpUnclamped(start, targetLocal, eased);
            yield return null;
        }
        tr.localPosition = targetLocal;
    }

    private IEnumerator DestroyAfter(GameObject obj, float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);

        if (moveCoroutines.ContainsKey(obj))
        {
            moveCoroutines.Remove(obj);
        }

        if (fadeCoroutines.ContainsKey(obj))
        {
            fadeCoroutines.Remove(obj);
        }

        if (obj != null)
            Destroy(obj);
    }
}