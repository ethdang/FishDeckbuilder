using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayerHand : MonoBehaviour
{
    public List<CardData> currentCards = new();

    public CardData selectedCard;

    public int startingHandSize = 5;
    public int handLimit = int.MaxValue;
    public bool isPlayingCard = false;
    public bool canPlayCard = false;

    private CardManager cardManager;
    private PlayerDeck playerDeck;
    private PlayerHandUI handUI;

    void Awake()
    {
        cardManager = FindFirstObjectByType<CardManager>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        handUI = FindFirstObjectByType<PlayerHandUI>();

        if (handUI == null)
            Debug.LogWarning("PlayerHand: handUI not found in scene. Draw visuals will fallback to AddCard.");
    }

    [ContextMenu("Draw Test Card")]
    private void DrawTestCard()
    {
        DrawCard();
    }

    public void DrawCard()
    {
        if (IsHandFull())
            return;

        CardData drawnCard = playerDeck.DrawCard();

        if (drawnCard == null)
            return;

        // Add to logical hand immediately
        currentCards.Add(drawnCard);

        // Start visualization: enqueue the reveal so draws animate sequentially
        if (handUI == null)
            handUI = FindFirstObjectByType<PlayerHandUI>();

        if (handUI != null)
        {
            handUI.EnqueueReveal(drawnCard);
        }
        else
        {
            // Fallback to just creating a visual if handUI is missing
            Add(drawnCard);
        }
    }

    public void PlayCardFromHand(CardData card)
    {
        if (isPlayingCard) return;

        currentCards.Remove(card);

        if (playerDeck == null)
            playerDeck = FindFirstObjectByType<PlayerDeck>();

        if (playerDeck != null)
            playerDeck.AddToDiscard(card);

        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();

        if (cardManager != null)
            cardManager.PlayCard(card);
    }

    public IEnumerator PlayCardFromHandCoroutine(CardData card)
    {
        // If cardManager isn't set, try to find it
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();

        // Remove from logical hand immediately (same as PlayCardFromHand)
        currentCards.Remove(card);

        if (playerDeck == null)
            playerDeck = FindFirstObjectByType<PlayerDeck>();

        if (playerDeck != null)
            playerDeck.AddToDiscard(card);

        if (cardManager != null)
        {
            // Wait for the entire play routine (including DelayEffect coroutines) to complete.
            yield return StartCoroutine(cardManager.PlayCardRoutine(card));
        }

        // Clear the playing lock now that all visual + effect work is done.
        isPlayingCard = false;
    }

    public void Add(CardData newCard)
    {
        if (IsHandFull())
            return;

        currentCards.Add(newCard);

        if (handUI == null)
            handUI = FindFirstObjectByType<PlayerHandUI>();

        if (handUI != null)
            handUI.AddCard(newCard);
    }

    public void Remove(CardData card)
    {
        currentCards.Remove(card);
    }

    // public void DiscardHand()
    // {
    //     List<CardData> removed = DiscardAllInstant();

    //     if (playerDeck == null)
    //         playerDeck = FindFirstObjectByType<PlayerDeck>();

    //     if (playerDeck != null)
    //     {
    //         foreach (var c in removed)
    //             playerDeck.AddToDiscard(c);
    //     }

    //     if (handUI == null)
    //         handUI = FindFirstObjectByType<PlayerHandUI>();

    //     if (handUI != null)
    //     {
    //         StartCoroutine(handUI.DiscardCardsAnimated());
    //     }

    // }

    public List<CardData> DiscardHand(bool addToDiscard = false)
    {
        List<CardData> removed = new List<CardData>(currentCards);
        currentCards.Clear();

        if (addToDiscard)
        {
            if (playerDeck == null)
                playerDeck = FindFirstObjectByType<PlayerDeck>();

            if (playerDeck != null)
            {
                foreach (var c in removed)
                    playerDeck.AddToDiscard(c);
            }
        }

        return removed;
    }

    public void DrawToStartingHandSize()
    {
        // Debug.Log(currentCards.Count);
        int safety = 50;

        while (currentCards.Count < startingHandSize && safety-- > 0)
        {
            DrawCard();
        }
    }

    public bool IsHandFull()
    {
        return currentCards.Count >= handLimit;
    }
}