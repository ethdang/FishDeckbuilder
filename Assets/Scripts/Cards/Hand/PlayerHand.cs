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
        Debug.Log($"[PlayerHand {GetInstanceID()}] PlayCardFromHand called for '{card?.cardName}'. currentCards count before: {currentCards.Count}");

        if (!currentCards.Contains(card))
        {
            // Print a helpful diagnostic of the currentCards contents
            string names = currentCards.Count == 0 ? "<empty>" :
                string.Join(", ", currentCards.ConvertAll(c => c != null ? c.cardName : "<null>"));
            Debug.LogWarning($"[PlayerHand {GetInstanceID()}] PlayCardFromHand: card not found in currentCards. card='{card?.cardName}'. currentCards=[{names}]");
            return;
        }

        currentCards.Remove(card);
        Debug.Log($"[PlayerHand {GetInstanceID()}] Removed card from currentCards. New count: {currentCards.Count}");

        // Update game state (discard + execution)
        if (playerDeck == null)
            playerDeck = FindFirstObjectByType<PlayerDeck>();

        if (playerDeck != null)
            playerDeck.AddToDiscard(card);
        else
            Debug.LogWarning($"[PlayerHand {GetInstanceID()}] playerDeck is null; can't add to discard.");

        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();

        if (cardManager != null)
            cardManager.PlayCard(card);
        else
            Debug.LogWarning($"[PlayerHand {GetInstanceID()}] cardManager is null; can't execute card.");

        Debug.Log($"[PlayerHand {GetInstanceID()}] PlayCardFromHand finished. Cards remaining: {currentCards.Count}");

        // NOTE: do NOT attempt to remove visuals here. Visual removal is handled by animation callbacks
        // which will call handUI.RemoveCardObject(gameObject) to remove the exact GameObject instance.
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