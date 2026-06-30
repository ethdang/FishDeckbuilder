using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EncounterRevealArea : MonoBehaviour
{
    [SerializeField] private RectTransform uiParent;
    [SerializeField] private GameObject encounterCardPrefab;

    [Header("Layout")]
    [SerializeField] private float verticalSpacing = 65f;

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
        for (int i = 0; i < activeCards.Count; i++)
        {
            EncounterCardUI card = activeCards[i];

            float cardHeight =
                card.GetComponent<RectTransform>().rect.height;

            float overlap = 40f; // tune this

            card.TargetPosition =
                new Vector2(
                    0f,
                    -(cardHeight - overlap) * i);

            card.transform.SetAsLastSibling();
        }
    }
}