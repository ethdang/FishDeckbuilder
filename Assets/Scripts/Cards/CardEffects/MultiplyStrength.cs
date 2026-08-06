using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Multiply Strength")]
public class MultiplyStrength : CardEffect
{
    public float amount;
    public bool roundDown = true;

    public override void Execute(CardContext context)
    {
        float currentStrength = context.resource.FishingStrength;
        float multipliedStrength = currentStrength * amount;
        int finalStrength;

        if (roundDown)
            finalStrength = (int)Math.Floor(multipliedStrength);
        else
            finalStrength = (int)Math.Ceiling(multipliedStrength);

        context.resource.SetStrength(finalStrength);
    }

    public override string ToString()
    {
        return $"Strength x{amount}";
    }
}