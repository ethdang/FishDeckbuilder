using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Add Focus")]
public class AddFocus : CardEffect
{
    public int amount;

    public override void Execute(CardContext context)
    {
        context.resource.AddFocus(amount);
    }
}