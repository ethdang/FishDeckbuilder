using UnityEngine;
using System.Collections.Generic;

public class EncounterDeck : MonoBehaviour
{
    public List<EncounterCardData> drawPile = new List<EncounterCardData>();
    public List<EncounterCardData> discardPile = new List<EncounterCardData>();

    private EncounterRevealArea revealArea;

    void Awake()
    {
        revealArea = FindAnyObjectByType<EncounterRevealArea>();
    }

    public EncounterCardData DrawCard()
    {
        if (drawPile.Count == 0)
        {
            Debug.LogError("Empty Deck");
            return null;
        }

        EncounterCardData drawnCard = drawPile[0];

        drawPile.Remove(drawnCard);
        discardPile.Add(drawnCard);

        return drawnCard;
    }

    public void ShuffleDeck()
    {
        int count = drawPile.Count;
        for (int i = 0; i < count - 1; i++)
        {
            // Pick a random index from i to the end of the list
            int randomIndex = Random.Range(i, count);
            
            // Swap the elements
            EncounterCardData temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }
}
