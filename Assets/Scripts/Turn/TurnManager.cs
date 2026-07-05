using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public int turn { get; private set; } = 1;

    public List<PendingCard> pendingCards = new();
    private PlayerHand playerHand;
    private PlayerHandUI handUI;
    private PlayerDeck playerDeck;
    private CardManager cardManager;

    void Awake()
    {
        playerHand = FindFirstObjectByType<PlayerHand>();
        handUI = FindFirstObjectByType<PlayerHandUI>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        cardManager = FindFirstObjectByType<CardManager>();
    }

    public void EndTurn()
    {
        StartCoroutine(EndTurnRoutine());
    }


    private IEnumerator EndTurnRoutine()
    {
        Debug.Log($"Before discard: {playerHand.currentCards.Count}");
        yield return handUI.DiscardCardsAnimated();
        Debug.Log($"After discard: {playerHand.currentCards.Count}");

        playerHand.DrawToHandLimit();
        Debug.Log($"After draw: {playerHand.currentCards.Count}");

        turn++;

        for (int i = pendingCards.Count - 1; i >= 0; i--)
        {
            pendingCards[i].turnsRemaining--;

            if (pendingCards[i].turnsRemaining <= 0)
            {
                cardManager.ExecuteCard(pendingCards[i].card); 
                
                // play an animation of the card showing up on screen 
                // to add some sort of visualization later on

                pendingCards.RemoveAt(i);
            }
        }
    }

    public void Reset()
    {
        turn = 1;
    }
}

[System.Serializable]
public class PendingCard
{
    public CardData card;
    public int turnsRemaining;
}