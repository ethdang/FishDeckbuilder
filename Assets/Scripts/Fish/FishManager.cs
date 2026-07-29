using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FishManager : MonoBehaviour
{
    public List<FishData> waitingFish = new();
    public FishData activeFish;

    // private PlayerRod currentRod;
    private bool isActivatingFish;

    private ActiveFishVisual fishVisual;
    private CardManager cardManager;
    private EncounterRevealArea encounterRevealArea;
    private PlayZoneUI playZoneUI;
    private PlayerResource playerResource;

    void Awake()
    {
        cardManager = FindFirstObjectByType<CardManager>();
        fishVisual = FindFirstObjectByType<ActiveFishVisual>();
        encounterRevealArea = FindFirstObjectByType<EncounterRevealArea>();
        playZoneUI = FindFirstObjectByType<PlayZoneUI>();
        playerResource = FindFirstObjectByType<PlayerResource>();
        // currentRod = FindFirstObjectByType<PlayerRod>();
    }

    // public FishData CatchFish()
    // {
    //     if (TryCatch)
    // }

    public void RevealFish(FishData fish)
    {
        RegisterFish(fish);
        
        if (waitingFish.Count == 1)
        {
            StartCoroutine(SetActiveFish());
        }
    }

    public void RegisterFish(FishData fish)
    {
        waitingFish.Add(fish);

        if (activeFish == null && !isActivatingFish)
        {
            StartCoroutine(SetActiveFish());
        }
    }

    public void RemoveFish(FishData fish)
    {
        if (fish == waitingFish[0])
        {
        //to do: make fish go to discard
        }

        waitingFish.Remove(fish);
    }

    private IEnumerator SetActiveFish()
    {
        isActivatingFish = true;

        FishData fish = waitingFish[0];

        yield return StartCoroutine(
            encounterRevealArea.MoveFishToPlayZone(fish)
        );

        activeFish = fish;

        fishVisual.ShowFish(fish);
        playZoneUI.UpdateStrength(playerResource.FishingStrength);
        playZoneUI.UpdateFishDurationIcons(fish.fishTurnDuration);

        isActivatingFish = false;
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