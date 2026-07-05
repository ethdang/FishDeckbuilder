using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public string description;
    public int cost; // COST // ex. 1 focus
    public int turnDelay = 0; // WHEN // ex. turn 0 executes this turn, turn 1 executes in the next turn\
    public CardAbility abilityType; // WHAT // ex. draw until
    public int amount;  // HOW MUCH // ex. draw 5 cards
    public EncounterCardCategory targetType; // DRAW UNTIL // draw until ??? in the encounter deck
    public DeckType targetDeck; // WHERE // ex. draw in the encounter deck   
}

public enum CardAbility
{
    Reveal,
    DrawUntil,
    Draw,
    Shuffle,
    AddFocus,
    IncreaseMaxFocus,

    //other things too like bait, so not just encounter cards
}

public enum DeckType
{
    Encounter,
    Player
}
