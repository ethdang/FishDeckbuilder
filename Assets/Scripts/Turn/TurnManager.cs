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
        StartCoroutine(EndTurnRoutine());
    }


    private IEnumerator EndTurnRoutine()
    {
        StartCoroutine(handUI.DiscardCardsAnimated());

        int safety = 0;

        while (handUI.isDiscarding && safety <= 20)
        {
            safety++;
            yield return null;   
        }

        playerHand.DrawToStartingHandSize();

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