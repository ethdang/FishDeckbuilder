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

    private bool isDiscarding;
    private float currentTotalFanAngle;
    private float currentRadius;

    void Awake()
    {
        playerHand = FindFirstObjectByType<PlayerHand>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
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
        GameObject obj = Instantiate(cardPrefab, objectParent);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = spawnPosition.anchoredPosition;

        CardUI ui = obj.GetComponent<CardUI>();
        ui.Initialize(card, playerHand);

        activeObjects.Add(obj);

        LayoutCards(false);
    }

    public void RemoveCard(CardData card)
    {
        for (int i = 0; i < activeObjects.Count; i++)
        {
            CardUI ui = activeObjects[i].GetComponent<CardUI>();

            if (ui.Card == card)
            {
                Destroy(activeObjects[i]);
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

        activeObjects.Remove(cardObject);
        LayoutCards(false);
    }

    void LateUpdate()
    {
        if (isDiscarding)
            return;

        LayoutCards(false);
    }

    public IEnumerator DiscardCardsAnimated()
    {
        isDiscarding = true;

        while (activeObjects.Count > 0)
        {
            GameObject obj = activeObjects[0];
            activeObjects.RemoveAt(0);

            CardUI ui = obj.GetComponent<CardUI>();
            

            ui.isAnimating = true;

            yield return obj.GetComponent<CardActionAnimation>().AnimateTo(
                objectParent,
                discardPile,
                null
            );

            Destroy(obj);

            yield return new WaitForSeconds(0.05f);
        }

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
            playerHand.Remove(ui.Card);

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