using UnityEngine;

[CreateAssetMenu(fileName = "EncounterCardData", menuName = "Cards/Encounter Card Data")]
public class EncounterCardData : ScriptableObject
{
    public CardCategory category; // UNTIL WHAT TYPE // ex. draw until water
    public FishData fishData;
    public bool IsFish => category == CardCategory.Encounter_Fish;
}