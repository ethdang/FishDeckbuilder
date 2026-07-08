using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Card Effects/Draw Cards")]
public class DrawCards : CardEffect
{
    public int amount = 0;  // HOW MUCH // ex. draw 5 cards
    public DeckType targetDeck; // WHERE // ex. draw in the encounter deck  
    // public int turnDelay = 0; // WHEN // ex. turn 0 executes this turn, turn 1 executes in the next turn

    public override void Execute(CardContext context)
    {
        if (targetDeck == DeckType.Encounter)
        {
            DrawEncounterCards(context, amount);
        }
        else
        {
            for (int i = 0; i < amount; i++)
                context.playerHand.DrawCard();
        }
    }

    public void DrawEncounterCards(CardContext context, int amount)
    {
        List<EncounterCardData> cards = new();

        for (int i = 0; i < amount; i++)
        {
            EncounterCardData card = context.encounterDeck.DrawCard();

            if (card == null)
                break;

            cards.Add(card);
        }

        context.revealArea.StartCoroutine(
            context.revealArea.RevealCards(cards));
    }
}