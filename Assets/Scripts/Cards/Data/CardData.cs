using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public string description;
    public CardAbility abilityType; // WHAT // draw until
    public int objectiveAmt;  // HOW MUCH // ex. draw 5 cards
    public EncounterCardType encounterObjectiveType; // UNTIL WHAT TYPE // ex. draw until water
    public DeckType targetDeck; // WHERE // ex. draw in the encounter deck   
}

public enum CardAbility
{
    Reveal,
    DrawUntil,
    Draw,
    Shuffle

    //other things too like bait, so not just encounter cards
}

public enum DeckType
{
    Encounter,
    Player
}