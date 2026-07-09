using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public int turn { get; private set; } = 1;

    public List<PendingEffect> pendingEffects = new();

    private PlayerHand playerHand;
    private PlayerHandUI handUI;
    private PlayerDeck playerDeck;
    private CardManager cardManager;
    private ContextManager contextManager;

    private bool isEndingTurn = false;

    void Awake()
    {
        playerHand = FindFirstObjectByType<PlayerHand>();
        handUI = FindFirstObjectByType<PlayerHandUI>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        cardManager = FindFirstObjectByType<CardManager>();
        contextManager = FindFirstObjectByType<ContextManager>();
    }

    public void EndTurn()
    {
        if (isEndingTurn) return;
        isEndingTurn = true;
        StartCoroutine(EndTurnRoutine());
    }

    private IEnumerator EndTurnRoutine()
    {
        if (handUI != null)
        {
            // Wait for discard coroutine to finish (handUI will perform animations)
            yield return StartCoroutine(handUI.DiscardCardsAnimated());

            // optional buffer between discard and draw (configurable on handUI)
            float buffer = 0f;
            try { buffer = handUI.postDiscardBuffer; } catch { buffer = 0f; }
            if (buffer > 0f)
                yield return new WaitForSeconds(buffer);
        }

        if (playerHand != null)
            playerHand.DrawToStartingHandSize();

        if (cardManager != null)
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

        isEndingTurn = false;
    }

    public void Reset()
    {
        turn = 1;
    }
}

[System.Serializable]
public class PendingEffect
{
    public CardEffect effect;
    public int turnsRemaining;
}