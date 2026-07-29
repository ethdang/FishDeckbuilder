using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CardManager : MonoBehaviour
{
    public List<CardEffect> turnEffect = new List<CardEffect>(); // tracks all of the effects played in the turn
    public List<CardEffect> allEffect = new List<CardEffect>(); // tracks all of the effects played in the encounter
    public List<CardModifier> modifiers = new List<CardModifier>();

    public float effectPlayDelay = 0.5f;

    private EncounterDeck encounterDeck;
    private PlayerDeck playerDeck;
    private PlayerHand playerHand;
    private EncounterRevealArea revealArea;
    private PlayerResource playerResource;
    private TurnManager turnManager;
    private ContextManager contextManager;

    void Awake()
    {
        encounterDeck = FindFirstObjectByType<EncounterDeck>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        playerHand = FindFirstObjectByType<PlayerHand>();
        revealArea = FindFirstObjectByType<EncounterRevealArea>();
        playerResource = FindFirstObjectByType<PlayerResource>();
        turnManager = FindFirstObjectByType<TurnManager>();
        contextManager = FindFirstObjectByType<ContextManager>();
    }

    public void PlayCard(CardData card)
    {
        StartCoroutine(PlayCardRoutine(card));
    }

    public IEnumerator DelayEffect(CardEffect effect)
    {
        yield return new WaitForSeconds(effectPlayDelay);

        effect.Execute(contextManager.GetContext());
    }

    public IEnumerator PlayCardRoutine(CardData card)
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
                usedModifiers.Add(modifier);

            if (modifier.duration == ModifierDuration.NextCard)
                usedModifiers.Add(modifier);
        }

        // Spend cost immediately
        playerResource?.SpendFocus(cost);

        foreach (CardModifier modifier in usedModifiers)
            modifiers.Remove(modifier);

        // We'll track running delayed effect coroutines and wait until they all complete.
        int running = 0;

        foreach (CardEffect effect in effects)
        {
            if (effect.turnDelay > 0)
            {
                // delayed to future turn — queue it and don't run now
                Queue(effect);
                continue;
            }

            for (int i = 0; i < playCount; i++)
            {
                running++;
                // Start a coroutine that runs DelayEffect (which waits then executes) and decrements 'running' when done.
                StartCoroutine(RunEffectAndSignal(effect, () => running--));
            }

            LogEffect(effect);
        }

        // Wait until all effect coroutines finish
        yield return new WaitUntil(() => running == 0);
    }

    // Helper: runs DelayEffect(effect) and calls onDone when finished.
    private IEnumerator RunEffectAndSignal(CardEffect effect, System.Action onDone)
    {
        yield return StartCoroutine(DelayEffect(effect));
        onDone?.Invoke();
    }

    public void RemoveEndOfTurnModifiers()
    {
        modifiers.RemoveAll(
            modifier => modifier.duration == ModifierDuration.EndOfTurn);
    }

    public void RemoveModifier(CardModifier remove)
    {
        modifiers.Remove(remove);
    }

    public bool CanExecute(CardData card)
    {
        // Only allow play when player can afford AND no card is currently being played.
        bool canAfford = playerResource != null && playerResource.CanAfford(card.cost);
        bool handNotPlaying = playerHand == null || !playerHand.isPlayingCard;
        return canAfford && handNotPlaying && playerHand.canPlayCard;
    } // Later on can add "locked" effects to certain cards due to boss effects from legendary fishes

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
}