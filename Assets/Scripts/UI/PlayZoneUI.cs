using UnityEngine;
using TMPro;
using System.Collections.Generic;
public class PlayZoneUI : MonoBehaviour
{
    [SerializeField] private TMP_Text fishingStrength;
    [SerializeField] private TMP_Text focusAmount;

    [Header("Fish Duration Icons")]
    [SerializeField] private GameObject fishDurationIconPrefab;
    [SerializeField] private RectTransform fishDurationPanel;

    [SerializeField] private float iconSpacing = 30f;

    public List<DurationIconUI> icons = new List<DurationIconUI>();

    private PlayerResource resource;
    private FishManager fishManager;

    void Awake()
    {
        resource = FindFirstObjectByType<PlayerResource>();
        fishManager = FindFirstObjectByType<FishManager>();
    }

    public void UpdateStrength(int amount)
    {
        fishingStrength.text = $"Fishing Strength: {amount}";
        
        if (fishManager.activeFish != null)
        {
            fishingStrength.text += $"/{fishManager.activeFish.requiredStrength}";
        }
    }

    public void UpdateFocus(int current, int max)
    {
        focusAmount.text = $"Current Focus: {current}/{max}";
    }

    public void UpdateFishDurationIcons(int fishDuration)
    {
        foreach (DurationIconUI icon in icons)
            Destroy(icon.gameObject);

        icons.Clear();

        for (int i = 0; i < fishDuration; i++)
        {
            DurationIconUI newIcon =
                Instantiate(fishDurationIconPrefab, fishDurationPanel)
                .GetComponent<DurationIconUI>();

            icons.Add(newIcon);
        }

        LayoutIcons();
    }

    private void LayoutIcons()
    {
        float iconWidth = fishDurationIconPrefab.GetComponent<RectTransform>().rect.width;
        float spacing = iconWidth + iconSpacing;

        float startX = -(icons.Count - 1) * spacing * 0.5f;

        for (int i = 0; i < icons.Count; i++)
        {
            icons[i].SetTargetPosition(
                new Vector2(startX + i * spacing, 0f));
        }
    }
}
