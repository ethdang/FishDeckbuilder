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

        if (playerDeck == null)
            playerDeck = FindFirstObjectByType<PlayerDeck>();

        CardData drawnCard = playerDeck != null ? playerDeck.DrawCard() : null;

        if (drawnCard == null)
            return;

        // Add to logical hand immediately
        currentCards.Add(drawnCard);
        Debug.Log($"[PlayerHand] Drew '{drawnCard.cardName}'. currentCards count now {currentCards.Count}");

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
        Debug.Log($"[PlayerHand {GetInstanceID()}] PlayCardFromHand called for '{card?.cardName}'. currentCards count before: {currentCards.Count}");

        if (!currentCards.Contains(card))
        {
            string names = currentCards.Count == 0 ? "<empty>" :
                string.Join(", ", currentCards.ConvertAll(c => c != null ? c.cardName : "<null>"));
            Debug.LogWarning($"[PlayerHand {GetInstanceID()}] PlayCardFromHand: card not found in currentCards. card='{card?.cardName}'. currentCards=[{names}]");
            return;
        }

        currentCards.Remove(card);
        Debug.Log($"[PlayerHand {GetInstanceID()}] Removed card from currentCards. New count: {currentCards.Count}");

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

        // Visual removal is handled by CardUI animation callback (handUI.RemoveCardObject) — do not remove visuals here.
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
        else
            Debug.LogWarning("PlayerHand.Add: handUI missing, visual not created.");
    }

    public void Remove(CardData card)
    {
        currentCards.Remove(card);
    }

    public void DiscardHand()
    {
        List<CardData> removed = DiscardAllInstant();

        if (playerDeck == null)
            playerDeck = FindFirstObjectByType<PlayerDeck>();

        if (playerDeck != null)
        {
            foreach (var c in removed)
                playerDeck.AddToDiscard(c);
        }
        else
        {
            Debug.LogWarning("PlayerHand.DiscardHand: playerDeck is null; logical discard not updated.");
        }

        if (handUI == null)
            handUI = FindFirstObjectByType<PlayerHandUI>();

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