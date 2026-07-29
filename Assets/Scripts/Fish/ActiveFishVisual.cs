using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ActiveFishVisual : MonoBehaviour
{
    [Header("Fish Visual")]
    [SerializeField] private Image artwork;
    [SerializeField] private TMP_Text fishName;
    [SerializeField] private RectTransform fishTransform;

    [Header("Fish Move")]
    [SerializeField] private float moveRadius = 120f;
    [SerializeField] private float moveSpeed = 35f;
    [SerializeField] private float rotateAmount = 8f;
    [SerializeField] private float rotateSpeed = 1f;
    [SerializeField] private float scaleAmount = 0.03f;
    [SerializeField] private float scaleSpeed = 1.5f;

    [Header("Fish Fade")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("")]
    public bool showVisual = false;

    private CanvasGroup canvasGroup;
    private Vector2 startPos;
    private Vector2 homePosition;
    private Vector2 targetPos;
    private float facing = 1f;

    private float moveDuration = 2f;
    private float moveTimer = 0f;

    void Awake()
    {
        canvasGroup = FindFirstObjectByType<CanvasGroup>();
    }

    void Start()
    {
        canvasGroup.alpha = 0f;

        targetPos = fishTransform.anchoredPosition;
        homePosition = fishTransform.anchoredPosition;
        PickNewTarget();
    }

    void Update()
    {
        if (!showVisual) 
        {
            fishTransform.anchoredPosition = homePosition;
            return; 
        }

        moveTimer += Time.deltaTime;

        float t = moveTimer / moveDuration;
        t = Mathf.Clamp01(t);

        t = Mathf.SmoothStep(0f, 1f, t);

        fishTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

        if (t >= 1f)
        {
            PickNewTarget();
        }

        float rotation =
            Mathf.Sin(Time.time * rotateSpeed) * rotateAmount;

        fishTransform.localRotation =
            Quaternion.Euler(0,0,rotation);

        Vector2 direction = targetPos - fishTransform.anchoredPosition;

        float targetFacing = direction.x >= 0 ? -1f : 1f;

        facing = Mathf.Lerp(facing, targetFacing, 8f * Time.deltaTime);

        float breathe =
            1 + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;

        fishTransform.localScale = new Vector3(
            facing * breathe,
            breathe,
            1f);

        fishName.GetComponent<RectTransform>().localScale = new Vector3(facing, 1f, 1f);
    }

    public IEnumerator FadeVisual(float targetAlpha)
    {
        float startingAlpha = canvasGroup.alpha;
        canvasGroup.alpha = startingAlpha;

        float fadeTimer = 0f;

        while (fadeTimer < fadeDuration)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(startingAlpha, targetAlpha, fadeTimer / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startingAlpha, targetAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    void PickNewTarget()
    {
        startPos = fishTransform.anchoredPosition;
        targetPos = homePosition + Random.insideUnitCircle * moveRadius;
        moveTimer = 0f;
    }

    public void ShowFish(FishData fish)
    {
        showVisual = true;
        artwork.sprite = fish.fishSprite;
        fishName.text = fish.fishName;

        StartCoroutine(FadeVisual(1f));
    }

    public void HideFish()
    {
        showVisual = false;

        artwork.sprite = null;
        fishName.text = "";

        StartCoroutine(FadeVisual(0f));
    }
}