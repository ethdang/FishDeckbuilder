using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public int turnDelay;

    public abstract void Execute(CardContext context);
}
