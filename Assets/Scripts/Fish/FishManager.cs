using UnityEngine;
using System.Collections.Generic;

public class FishManager : MonoBehaviour
{
    public List<FishData> waitingFish = new();
    public FishData activeFish;

    // private PlayerRod currentRod;
    private ActiveFishVisual fishVisual;
    private CardManager cardManager;

    void Awake()
    {
        cardManager = FindFirstObjectByType<CardManager>();
        fishVisual = FindFirstObjectByType<ActiveFishVisual>();
        // currentRod = FindFirstObjectByType<PlayerRod>();
    }

    // public FishData CatchFish()
    // {
    //     if (TryCatch)
    // }

    public void RevealFish(FishData fish)
    {
        RegisterFish(fish);
        
        if (waitingFish.Count == 0)
        {
            SetActiveFish(fish);
        }
    }

    public void RegisterFish(FishData newFish)
    {
        waitingFish.Add(newFish);
    }

    public void SetActiveFish(FishData fish)
    {
        activeFish = fish;
        fishVisual.UpdateFish(fish);
    }

    public bool TryCatch(int fishingStrength)
    {
        List<CardModifier> usedModifiers = new();
        int modifiedStrength = fishingStrength;

        foreach (CardModifier modifier in cardManager.modifiers)
        {
            modifiedStrength = modifier.ModifyFishingStrength(modifiedStrength);

            modifier.remainingUses--;

            if (modifier.remainingUses <= 0)
            {
                usedModifiers.Add(modifier);
            }

            if (modifier.duration == ModifierDuration.NextCard)
                usedModifiers.Add(modifier);
        }

        foreach (CardModifier modifier in usedModifiers)
        {
            cardManager.RemoveModifier(modifier); // call a function instead of directly changing to prevent tracking issues
        }

        return modifiedStrength >= activeFish.requiredStrength;
    }
}