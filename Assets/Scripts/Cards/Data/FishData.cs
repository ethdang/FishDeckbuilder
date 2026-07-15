using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "FishData", menuName = "Cards/Fish Data")]
public class FishData : ScriptableObject
{
    public string fishName;
    public Sprite fishSprite;
    // public Rarities fishRarity; // make rarities once we decide on how much
    public int requiredStrength;
    public int fishTurnDuration;
}
