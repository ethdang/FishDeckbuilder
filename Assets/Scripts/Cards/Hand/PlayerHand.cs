using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    public List<CardData> currentCards = new();

    public CardData selectedCard;

    public int startingHandSize = 5;
    public int handLimit = int.MaxValue;

    private CardManager cardManager;
    private PlayerDeck playerDeck;
    private PlayerHandUI handUI;

    void Awake()
    {   
        cardManager = FindFirstObjectByType<CardManager>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        handUI = FindAnyObjectByType<PlayerHandUI>();
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

        Add(drawnCard);
    }

    public void PlayCardFromHand(CardData card)
    {
        if (!currentCards.Contains(card))
            return;

        Debug.Log($"Played {card.cardName}");

        currentCards.Remove(card);

        playerDeck.AddToDiscard(card);
        cardManager.PlayCard(card);

        Debug.Log($"Cards remaining: {currentCards.Count}");

        handUI.RemoveCard(card); // ONLY visual removal here
    }

    public void Add(CardData newCard)
    {
        if (IsHandFull())
            return;

        currentCards.Add(newCard);

        handUI.AddCard(newCard);
    }

    public void Remove(CardData card)
    {
        currentCards.Remove(card);
    }

    public void DiscardHand()
    {
        List<CardData> removed = DiscardAllInstant();

        // Ensure we have a playerDeck reference
        if (playerDeck == null)
            playerDeck = FindFirstObjectByType<PlayerDeck>();

        // Move them to the logical discard pile so game state is correct immediately
        if (playerDeck != null)
        {
            foreach (var c in removed)
                playerDeck.AddToDiscard(c);
        }
        else
        {
            Debug.LogWarning("PlayerHand.DiscardHand: playerDeck is null; logical discard not updated.");
        }

        // Ensure we have a handUI reference
        if (handUI == null)
            handUI = FindFirstObjectByType<PlayerHandUI>();

        // Start the existing UI coroutine to animate cards moving to the discard pile.
        // IMPORTANT: start it as a coroutine so the UI animation actually runs.
        if (handUI != null)
        {
            StartCoroutine(handUI.DiscardCardsAnimated());
        }
        else
        {
            Debug.LogWarning("PlayerHand.DiscardHand: handUI is null; visual discard will not animate.");
        }
    }

    public List<CardData> DiscardAllInstant()
    {
        List<CardData> removed = new List<CardData>(currentCards);
        currentCards.Clear();
        return removed;
    }

    public void DrawToStartingHandSize()
    {
        int safety = 20;

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