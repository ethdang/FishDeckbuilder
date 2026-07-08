using UnityEngine;

public class SetFocusModifier : CardModifier
{
    public int newCost;

    public override int ModifyCost(int cost)
    {
        return newCost;
    }
}