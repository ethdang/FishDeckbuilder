using UnityEngine;

[CreateAssetMenu(menuName = "Card Modifiers/Reduce Next Card Focus Cost")]
public class ReduceNextCardCost : CardModifier
{    
    public int reduceAmount;

    public override void Execute(CardContext context)
    {
        context.cardManager.modifiers.Add(this);
    }
    public override int ModifyCost(int cost)
    {
        return Mathf.Max(cost - reduceAmount, 0);
    }

    public override string ToString()
    {
        return $"{reduceAmount.ToString("+0;-0;0")} Next Card Cost";
    }
}