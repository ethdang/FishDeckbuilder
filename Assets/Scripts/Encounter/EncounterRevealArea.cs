using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EncounterRevealArea : MonoBehaviour
{
    [SerializeField] private RectTransform uiParent;
    [SerializeField] private GameObject encounterCardPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Layout")]
    [SerializeField] private float verticalPadding = 200f;

    [Header("Spawn")]
    [SerializeField] private RectTransform deckSpawnPoint;

    public List<EncounterCardUI> activeCards = new();

    public void RevealCard(EncounterCardData card)
    {
        GameObject obj = Instantiate(encounterCardPrefab, uiParent);

        EncounterCardUI ui = obj.GetComponent<EncounterCardUI>();

        ui.Initialize(card, deckSpawnPoint);

        activeCards.Insert(0, ui);

        LayoutCards();

        ui.StartReveal();
    }

    public IEnumerator RevealCards(List<EncounterCardData> cards)
    {
        foreach (var card in cards)
        {
            GameObject obj = Instantiate(encounterCardPrefab, uiParent);

            EncounterCardUI ui = obj.GetComponent<EncounterCardUI>();

            ui.Initialize(card, deckSpawnPoint);

            activeCards.Insert(0, ui);

            LayoutCards();

            ui.StartReveal();

            // Wait until THIS card finishes before revealing the next.
            yield return new WaitUntil(() => ui.IsFinished);
        }
    }

    public void RemoveCard(EncounterCardUI card)
    {
        if (!activeCards.Contains(card))
            return;

        activeCards.Remove(card);

        Destroy(card.gameObject);

        LayoutCards();
    }

    public void Clear()
    {
        foreach (EncounterCardUI card in activeCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        activeCards.Clear();
    }

    public void LayoutCards()
    {
        float cardHeight = 0f;

        if (activeCards.Count > 0)
            cardHeight = activeCards[0].GetComponent<RectTransform>().rect.height;

        float overlap = 150f;
        float spacing = cardHeight - overlap;

        // Height of the stack
        float stackHeight = cardHeight + (activeCards.Count - 2) * spacing - spacing;

        // Content height
        float contentHeight = stackHeight + verticalPadding * 2;

        uiParent.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            contentHeight);

        // Shift so the top padding matches the bottom padding.
        float offset = stackHeight * 0.5f;

        for (int i = 0; i < activeCards.Count; i++)
        {
            EncounterCardUI card = activeCards[i];

            card.TargetPosition = new Vector2(
                0f,
                i * spacing - offset);

            card.transform.SetSiblingIndex(activeCards.Count - 1 - i);
        }

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}