using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Card Effects/Increase Max Focus")]
public class IncreaseMaxFocus : CardEffect
{
    public int amount;

    public override void Execute(CardContext context)
    {
        context.resource.SetMaxFocus(context.resource.MaxFocus + amount);
    }
}