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
        int playCount = 1;

        List<CardEffect> effects = new(card.effects);
        List<CardModifier> usedModifiers = new();

        foreach (CardModifier modifier in modifiers)
        {
            playCount = modifier.ModifyPlayCount(playCount);
            cost = modifier.ModifyCost(cost);
            effects = modifier.ModifyEffects(effects);

            modifier.remainingUses--;

            if (modifier.remainingUses <= 0)
            {
                usedModifiers.Add(modifier);
            }

            if (modifier.duration == ModifierDuration.NextCard)
                usedModifiers.Add(modifier);

        }

        playerResource.SpendFocus(cost);

        foreach (CardModifier modifier in usedModifiers)
        {
            modifiers.Remove(modifier);
        }

        foreach (CardEffect effect in effects)
        {
            if (effect.turnDelay > 0)
            {
                Queue(effect);
                continue;
            }

            for (int i = 0; i < playCount; i++)
            {
                effect.Execute(contextManager.GetContext());
            }

            LogEffect(effect);
        }
    }

    public void RemoveEndOfTurnModifiers()
    {
        modifiers.RemoveAll(
            modifier => modifier.duration == ModifierDuration.EndOfTurn);
    }

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