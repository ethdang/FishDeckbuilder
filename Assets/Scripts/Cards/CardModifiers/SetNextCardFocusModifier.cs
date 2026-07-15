using UnityEngine;

[CreateAssetMenu(menuName = "Card Modifiers/Set Next Card Focus Cost")]
public class SetNextCardFocusModifier : CardModifier
{    
    public int newCost;

    public override void Execute(CardContext context)
    {
        context.cardManager.modifiers.Add(this);
    }
    public override int ModifyCost(int cost)
    {
        return newCost;
    }
}