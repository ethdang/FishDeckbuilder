public class ShuffleDeck : CardEffect
{
    public DeckType targetDeck; // WHERE // ex. draw in the encounter deck  
    // public int turnDelay = 0; // WHEN // ex. turn 0 executes this turn, turn 1 executes in the next turn

    private EncounterDeck encounterDeck;
    private PlayerDeck playerDeck;

    void Awake()
    {
        encounterDeck = FindFirstObjectByType<EncounterDeck>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
    }

    public override void Execute(CardContext context)
    {
        switch (targetDeck)
        {
            case DeckType.Encounter:
                encounterDeck.ShuffleDeck();
                break;
            case DeckType.Player:
                playerDeck.ShuffleDeck();
                break;
        }
    }
}