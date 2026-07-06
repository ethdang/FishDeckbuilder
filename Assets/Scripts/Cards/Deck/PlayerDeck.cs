using UnityEngine;
using System.Collections.Generic;

public class PlayerDeck : MonoBehaviour
{
    public List<CardData> drawPile = new();
    public List<CardData> discardPile = new();

    public CardData DrawCard()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogError("Player deck is empty.");
            return null;
        }

        CardData drawnCard = drawPile[0];

        drawPile.RemoveAt(0);

        return drawnCard;
    }

    public void ShuffleDeck()
    {
        int count = drawPile.Count;

        for (int i = 0; i < count - 1; i++)
        {
            int randomIndex = Random.Range(i, count);

            CardData temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    public void ReshuffleDiscardIntoDeck()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        ShuffleDeck();
    }

    public void AddToDiscard(CardData card)
    {
        discardPile.Add(card);
    }
}