using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Card Effects/Reduce Pending Delay of All Cards")]
public class ReducePendingDelay : CardEffect
{
    public int reduceAmount;

    public override void Execute(CardContext context)
    {
        foreach (PendingEffect pending in context.turnManager.pendingEffects)
        {
            pending.turnsRemaining -= reduceAmount;
        }
    }
}