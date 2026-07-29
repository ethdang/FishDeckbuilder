using TMPro;
using UnityEngine;

public class PlayerResource : MonoBehaviour
{
    public int MaxFocus { get; private set; } = 3;
    public int CurrentFocus { get; private set; } = 3;
    public int FishingStrength { get; private set; } = 0;

    private PlayZoneUI playZoneUI;

    void Awake()
    {
        playZoneUI = FindFirstObjectByType<PlayZoneUI>();
    }

    public bool CanAfford(int cost)
    {
        return CurrentFocus >= cost;
    }

    public bool SpendFocus(int cost)
    {
        if (!CanAfford(cost))
            return false;

        AddFocus(-cost);
        return true;
    }

    public void AddFocus(int amount)
    {
        CurrentFocus = Mathf.Min(CurrentFocus + amount, MaxFocus);

        playZoneUI.UpdateFocus(CurrentFocus, MaxFocus);
    }

    public void AddStrength(int amount)
    {
        FishingStrength += amount;
        
        playZoneUI.UpdateStrength(FishingStrength);
    }

    public void ResetStrength()
    {
        FishingStrength = 0;

        playZoneUI.UpdateStrength(FishingStrength);
    } 

    public void RestoreToMaxFocus()
    {
        CurrentFocus = MaxFocus;

        playZoneUI.UpdateFocus(CurrentFocus, MaxFocus);
    }

    public void SetMaxFocus(int newMax)
    {
        MaxFocus = newMax;
        CurrentFocus = Mathf.Min(CurrentFocus, MaxFocus);

        playZoneUI.UpdateFocus(CurrentFocus, MaxFocus);
    }
}
