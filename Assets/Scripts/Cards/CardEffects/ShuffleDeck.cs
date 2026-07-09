using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Shuffle Deck")]
public class ShuffleDeck : CardEffect
{
    public DeckType targetDeck; // WHERE // ex. draw in the encounter deck  
    // public int turnDelay = 0; // WHEN // ex. turn 0 executes this turn, turn 1 executes in the next turn

    public override void Execute(CardContext context)
    {
        switch (targetDeck)
        {
            case DeckType.Encounter:
                context.encounterDeck.ShuffleDeck();
                break;
            case DeckType.Player:
                context.playerDeck.ShuffleDeck();
                break;
        }
    }
}