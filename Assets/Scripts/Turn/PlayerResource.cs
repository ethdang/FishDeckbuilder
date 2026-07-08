using TMPro;
using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    public int MaxFocus { get; private set; } = 3;
    public int CurrentFocus { get; private set; } = 3;

    public bool CanAfford(int cost)
    {
        return CurrentFocus >= cost;
    }

    public bool SpendFocus(int cost)
    {
        if (!CanAfford(cost))
            return false;

        CurrentFocus -= cost;
        return true;
    }

    public void GainFocus(int amount)
    {
        CurrentFocus = Mathf.Min(CurrentFocus + amount, MaxFocus);
    }

    public void RestoreToMaxFocus()
    {
        CurrentFocus = MaxFocus;
    }

    public void SetMaxFocus(int newMax)
    {
        MaxFocus = newMax;
        CurrentFocus = Mathf.Min(CurrentFocus, MaxFocus);
    }
}
