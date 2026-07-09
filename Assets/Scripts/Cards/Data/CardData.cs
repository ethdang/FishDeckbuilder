using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardData", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public string description;
    public CardCategory category;
    public int cost; // COST // ex. 1 focus
    public List<CardEffect> effects; // WHAT // which effects does this card have?
}

public enum CardAbility
{
    None,
    DrawUntil,
    Draw,
    ShuffleDeck,
    AddFocus,
    IncreaseMaxFocus,
    SetFocusNextCard

    //other things too like bait, so not just encounter cards
}

public enum DeckType
{
    Encounter,
    Player
}

public enum CardCategory
{
    Encounter_Water,
    Encounter_Fish,
    Player_Fishing,
    Player_Cycle,
    Player_Support
}
