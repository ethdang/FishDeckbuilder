using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class EffectPanelUI : MonoBehaviour
{
    public List<GameObject> activeCurrentObjs = new();
    public List<GameObject> activeUpcomingObjs = new();

    [SerializeField] private GameObject currentEffectPrefab;
    [SerializeField] private GameObject upcomingEffectPrefab;

    [SerializeField] private RectTransform currentEffectParent;
    [SerializeField] private RectTransform upcomingEffectParent;

    private TurnManager turnManager;

    void Awake()
    {
        turnManager = FindFirstObjectByType<TurnManager>();
    }

    public void UpdateUpcomingEffects()
    {
        foreach (GameObject obj in activeUpcomingObjs)
            Destroy(obj);

        activeUpcomingObjs.Clear();

        foreach (PendingEffect effect in turnManager.pendingEffects)
        {
            GameObject newUpcomingEffect = Instantiate(upcomingEffectPrefab, upcomingEffectParent);

            activeUpcomingObjs.Add(newUpcomingEffect);

            GameObject delayTextObj = newUpcomingEffect.GetComponentInChildren<DelayText>().gameObject;
            TMP_Text delayText = delayTextObj.GetComponent<TMP_Text>();

            delayText.text = effect.turnsRemaining.ToString();

            GameObject effectTextObj = newUpcomingEffect.GetComponentInChildren<EffectText>().gameObject;
            TMP_Text effectText = effectTextObj.GetComponent<TMP_Text>();

            effectText.text = effect.effect.ToString();
        }
    }
}