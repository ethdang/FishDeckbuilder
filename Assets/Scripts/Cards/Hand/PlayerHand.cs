using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    public List<CardData> currentCards = new();

    public CardData selectedCard;

    public int handLimit = 5;

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
        Debug.Log($"Cards remaining: {currentCards.Count}");

        currentCards.Remove(card);

        playerDeck.AddToDiscard(card);
        cardManager.PlayCard(card);

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
        handUI.DiscardCardsAnimated();
    }

    public List<CardData> DiscardAllInstant()
    {
        List<CardData> removed = new List<CardData>(currentCards);
        currentCards.Clear();
        return removed;
    }

    public void DrawToHandLimit()
    {
        int safety = 20;

        while (!IsHandFull() && safety-- > 0)
        {
            Debug.Log($"Drawing... Hand = {currentCards.Count}");

            DrawCard();
        }

        Debug.Log($"Finished drawing. Hand = {currentCards.Count}");
    }

    public bool IsHandFull()
    {
        return currentCards.Count >= handLimit;
    }
}