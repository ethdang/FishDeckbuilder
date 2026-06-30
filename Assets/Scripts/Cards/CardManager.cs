using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    private EncounterDeck encounterDeck;
    private EncounterRevealArea revealArea;

    [SerializeField] private CardData testCard;

    [ContextMenu("Test Draw Card")]
    public void TestCard()
    {
        PlayCard(testCard);
    }

    void Awake()
    {
        encounterDeck = FindFirstObjectByType<EncounterDeck>();
        revealArea = FindFirstObjectByType<EncounterRevealArea>();
    }

    public void PlayCard(CardData card)
    {
        switch (card.abilityType)
        {
            case CardAbility.Draw:
                DrawCards(card.amount);
                break;

            case CardAbility.DrawUntil:
                DrawUntil(card.targetType);
                break;
        }
    }

    private void DrawCards(int amount)
    {
        List<EncounterCardData> cards = new();

        for (int i = 0; i < amount; i++)
        {
            EncounterCardData card = encounterDeck.DrawCard();

            if (card == null)
                break;

            cards.Add(card);
        }

        StartCoroutine(revealArea.RevealCards(cards));
    }

    private void DrawUntil(EncounterCardCategory type)
    {
        List<EncounterCardData> cards = new();

        int safety = 50;

        while (safety-- > 0)
        {
            EncounterCardData card = encounterDeck.DrawCard();

            if (card == null)
                break;

            cards.Add(card);

            if (card.category == type)
                break;
        }

        StartCoroutine(revealArea.RevealCards(cards));
    }
}