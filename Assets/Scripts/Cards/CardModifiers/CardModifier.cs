using System.Collections.Generic;

public abstract class CardModifier : CardEffect
{
    public ModifierDuration duration;
    public int remainingUses = 1;

    public virtual int ModifyCost(int cost)
    {
        return cost;
    }

    public virtual List<CardEffect> ModifyEffects(List<CardEffect> effects)
    {
        return effects;
    }

    public virtual int ModifyDrawAmount(int amount)
    {
        return amount;
    }

    public virtual int ModifyPlayCount(int amount)
    {
        return amount;
    }
}
public enum ModifierDuration
{
    NextCard,
    EndOfTurn,
    EndOfEncounter,
    Permanent
}