using UnityEngine;

[CreateAssetMenu(fileName = "EncounterCardData", menuName = "Cards/Encounter Card Data")]
public class EncounterCardData : ScriptableObject
{
    public EncounterCardType cardType; // UNTIL WHAT TYPE // ex. draw until water
    public FishData fishData;
}


public enum EncounterCardType
{
    Fish,
    Water
}