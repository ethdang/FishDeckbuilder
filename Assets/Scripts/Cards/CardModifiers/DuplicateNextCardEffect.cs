using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Modifiers/Play Next Card Twice")]
public class PlayNextCardTwice : CardModifier
{
    public override void Execute(CardContext context)
    {
        context.cardManager.modifiers.Add(this);
    }

    public override int ModifyPlayCount(int playCount)
    {
        return playCount + 1;
    }

    public override string ToString()
    {
        return "Double Next Card";
    }
}