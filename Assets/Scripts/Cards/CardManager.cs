using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    private EncounterDeck encounterDeck;
    private EncounterRevealArea revealArea;
    private PlayerResource playerResource;
    private TurnManager turnManager;

    [SerializeField] private CardData testCard;

    void Awake()
    {
        encounterDeck = FindFirstObjectByType<EncounterDeck>();
        revealArea = FindFirstObjectByType<EncounterRevealArea>();
        playerResource = FindFirstObjectByType<PlayerResource>();
        turnManager = FindFirstObjectByType<TurnManager>();
    }

    public void PlayCard(CardData card)
    {
        playerResource.Spend(card.cost);

        if (card.turnDelay > 0)
        {
            PendingCard newPending = new();

            newPending.card = card;
            newPending.turnsRemaining = card.turnDelay;

            turnManager.pendingCards.Add(newPending);

            return;
        }

        ExecuteCard(card);
    }

    public void ExecuteCard(CardData card)
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

    public bool CanExecute(CardData card)
    {
        return playerResource.CanAfford(card.cost); // Later on can add "locked" effects to certain cards due to boss effects from legendary fishes
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