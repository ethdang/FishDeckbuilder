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
}