using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    public List<CardData> currentCards = new();

    public CardData selectedCard;

    public int handLimit = 10;

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
        if (card == null || !currentCards.Contains(card))
            return;

        currentCards.Remove(card);

        playerDeck.AddToDiscard(card);

        cardManager.PlayCard(card);

        handUI.UpdateHand();
    }

    public void Add(CardData newCard)
    {
        if (IsHandFull())
            return;

        currentCards.Add(newCard);

        handUI.UpdateHand();
    }

    public void Remove(CardData card)
    {
        currentCards.Remove(card);

        handUI.UpdateHand();
    }

    public bool IsHandFull()
    {
        return currentCards.Count >= handLimit;
    }
}