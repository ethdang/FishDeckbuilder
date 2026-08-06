using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Add Strength")]
public class AddStrength : CardEffect
{
    public int amount;

    public override void Execute(CardContext context)
    {
        context.resource.AddStrength(amount);
    }

    public override string ToString()
    {
        return $"{amount.ToString("+0;-0;0")} Strength";
    }
}