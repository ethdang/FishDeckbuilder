using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public int turn { get; private set; } = 1;

    public List<PendingEffect> pendingEffects = new();

    private bool isEndingTurn = false;

    private PlayerHand playerHand;
    private PlayerHandUI handUI;
    private PlayerDeck playerDeck;
    private CardManager cardManager;
    private ContextManager contextManager;
    private PlayerResource playerResource;
    private PlayZoneUI playZone;
    private FishManager fishManager;
    private EffectPanelUI effectPanelUI;

    void Awake()
    {
        playerHand = FindFirstObjectByType<PlayerHand>();
        handUI = FindFirstObjectByType<PlayerHandUI>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        cardManager = FindFirstObjectByType<CardManager>();
        contextManager = FindFirstObjectByType<ContextManager>();
        playerResource = FindFirstObjectByType<PlayerResource>();
        playZone = FindFirstObjectByType<PlayZoneUI>();
        fishManager = FindFirstObjectByType<FishManager>();
        effectPanelUI = FindFirstObjectByType<EffectPanelUI>();
    }

    void Start()
    {
        playerDeck.ShuffleDeck();
        Reset();
    }

    public void EndTurn()
    {
        if (isEndingTurn)
            return;

        isEndingTurn = true;
        StartCoroutine(EndTurnRoutine());
    }

    private IEnumerator EndTurnRoutine()
    {
        if (handUI != null)
        {
            yield return StartCoroutine(handUI.DiscardCardsAnimated());   
        }

        playerResource.RestoreToMaxFocus();

        playerHand.DrawToStartingHandSize();

        yield return new WaitUntil(() => !handUI.isRevealing);

        cardManager.RemoveEndOfTurnModifiers();

        turn++;

        for (int i = pendingEffects.Count - 1; i >= 0; i--)
        {
            pendingEffects[i].turnsRemaining--;

            if (pendingEffects[i].turnsRemaining <= 0)
            {
                pendingEffects[i].effect.Execute(contextManager.GetContext());

                pendingEffects.RemoveAt(i);
            }
        }

        FishData activeFish = fishManager.activeFish;

        if (activeFish != null)
        {
            activeFish.fishTurnDuration -= 1; // add modifiers that are able to affect this number too

            playZone.UpdateFishDurationIcons(activeFish.fishTurnDuration);        

            // if (activeFish.fishTurnDuration == 0)
                // fishManager.
        }

        effectPanelUI.UpdateUpcomingEffects();

        isEndingTurn = false;
    }

    public void Reset()
    {
        turn = 1;
        playerResource.ResetStrength();
        EndTurn();
    }
}

[System.Serializable]
public class PendingEffect
{
    public CardEffect effect;
    public int turnsRemaining;
}