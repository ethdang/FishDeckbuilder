using UnityEngine;
using System.Collections.Generic;

public class ContextManager : MonoBehaviour
{
    private PlayerHand playerHand;
    private PlayerDeck playerDeck;
    private EncounterDeck encounterDeck;
    private PlayerResource resource;
    private TurnManager turnManager;
    private EncounterRevealArea revealArea;
    

    void Awake()
    {
        playerHand = FindFirstObjectByType<PlayerHand>();
        playerDeck = FindFirstObjectByType<PlayerDeck>();
        encounterDeck = FindFirstObjectByType<EncounterDeck>();
        resource = FindFirstObjectByType<PlayerResource>();
        turnManager = FindFirstObjectByType<TurnManager>();
        revealArea = FindFirstObjectByType<EncounterRevealArea>();
    }

    public CardContext GetContext()
    {
        CardContext newContext = new CardContext();

        newContext.playerHand = playerHand;
        newContext.playerDeck = playerDeck;
        newContext.encounterDeck = encounterDeck;
        newContext.resource = resource;
        newContext.turnManager = turnManager;
        newContext.revealArea = revealArea;

        return newContext;
    }
}

public class CardContext
{
    public PlayerHand playerHand;
    public PlayerDeck playerDeck;
    public EncounterDeck encounterDeck;
    public PlayerResource resource;
    public TurnManager turnManager;
    public EncounterRevealArea revealArea;
}