using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Card Effects/Draw Until")]
public class DrawUntil : CardEffect
{
    public DeckType targetDeck; // WHERE // ex. draw in the encounter deck  
    // public int turnDelay = 0; // WHEN // ex. turn 0 executes this turn, turn 1 executes in the next turn
    public CardCategory targetCategory;

    public override void Execute(CardContext context)
    {
        switch (targetDeck)
        {
            case DeckType.Encounter:
                DrawUntilEncounterCards(context);
                break;
            case DeckType.Player:
                DrawUntilPlayerCards(context);
                break;
        }
    }

    public void DrawUntilEncounterCards(CardContext context)
    {
            List<EncounterCardData> cards = new();

        int safety = 50;

        while (safety-- > 0)
        {
            EncounterCardData card = context.encounterDeck.DrawCard();

            if (card == null)
                break;

            cards.Add(card);

            if (card.category == targetCategory)
                break;
        }

        context.revealArea.StartCoroutine(
            context.revealArea.RevealCards(cards));
    }

    public void DrawUntilPlayerCards(CardContext context)
    {
        int safety = 50;

        while (safety-- > 0)
        {
            CardData card = context.playerDeck.DrawCard();

            if (card.category == targetCategory)
                break;
        }
    }
}