using UnityEngine;

[CreateAssetMenu(menuName = "Card Modifiers/Temp Increase Fishing Strength")]
public class TempIncreaseFishingStrength : CardModifier
{    
    public int amount;

    public override void Execute(CardContext context)
    {
        context.cardManager.modifiers.Add(this);
    }
    public override int ModifyFishingStrength(int strength)
    {
        return strength + amount;
    }
    public override string ToString()
    {
        return $"{amount.ToString("+0;-0;0")} Strength";
    }
}