using UnityEngine;
using System.Collections.Generic;

public class CardManager : MonoBehaviour
{
    public List<CardEffect> turnEffect = new List<CardEffect>(); // tracks all of the effects played in the turn
    public List<CardEffect> allEffect = new List<CardEffect>(); // tracks all of the effects played in the encounter
    public List<CardModifier> modifiers = new List<CardModifier>();

    private EncounterDeck encounterDeck;
    private PlayerDeck playerDeck;
    private EncounterRevealArea revealArea;
    private PlayerResource playerResource;
    private TurnManager turnManager;
    private ContextManager contextManager;

    void Awake()
    {
        encounterDeck = FindFirstObjectByType<EncounterDeck>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        revealArea = FindFirstObjectByType<EncounterRevealArea>();
        playerResource = FindFirstObjectByType<PlayerResource>();
        turnManager = FindFirstObjectByType<TurnManager>();
        contextManager = FindFirstObjectByType<ContextManager>();
    }

    public void PlayCard(CardData card)
    {
        int cost = card.cost;

        foreach (CardModifier modifier in modifiers)
        {
            modifier.ModifyCost(cost);
        }

        playerResource.SpendFocus(cost);

        foreach (CardEffect effect in card.effects)
        {
            if (effect.turnDelay > 0)
            {
                Queue(effect);
                continue;
            }

            effect.Execute(contextManager.GetContext());

            LogEffect(effect);
        }
    }

    // public void ExecuteEffect(CardEffect effect)
    // {
    //     switch (effect.ability)
    //     {
    //         case CardAbility.Draw:
    //             DrawCards(effect.amount);
    //             break;

    //         case CardAbility.ShuffleDeck:
    //             Shuffle(effect.targetDeck);
    //             break;

    //         case CardAbility.DrawUntil:
    //             DrawUntil(effect.targetCategory);
    //             break;

    //         case CardAbility.AddFocus:
    //             playerResource.GainFocus(effect.amount);
    //             break;

    //         case CardAbility.IncreaseMaxFocus:
    //             playerResource.SetMaxFocus(playerResource.MaxFocus + effect.amount);
    //             break;

    //         case CardAbility.SetFocusNextCard:
    //             // we log the effect later on.
    //             break;
    //     }

        
    // }

    public bool CanExecute(CardData card)
    {
        return playerResource.CanAfford(card.cost); // Later on can add "locked" effects to certain cards due to boss effects from legendary fishes
    }

    public void Queue(CardEffect effect)
    {
        PendingEffect newPending = new();

        newPending.effect = effect;
        newPending.turnsRemaining = effect.turnDelay;

        turnManager.pendingEffects.Add(newPending);
    }

    public void LogEffect(CardEffect effect)
    {
        turnEffect.Add(effect);
        allEffect.Add(effect);
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

    private void Shuffle(DeckType deck)
    {
        switch (deck)
        {
            case DeckType.Encounter:
                encounterDeck.ShuffleDeck();
                break;
            case DeckType.Player:
                playerDeck.ShuffleDeck();
                break;
        }
    }

    private void DrawUntil(CardCategory type)
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