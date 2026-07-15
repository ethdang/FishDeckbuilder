using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActiveFishVisual : MonoBehaviour
{
    [SerializeField] private Image artwork;
    [SerializeField] private TMP_Text fishName;

    [SerializeField] private RectTransform fishTransform;

    [SerializeField] private float moveRadius = 120f;
    [SerializeField] private float moveSpeed = 35f;
    [SerializeField] private float rotateAmount = 8f;
    [SerializeField] private float rotateSpeed = 1f;
    [SerializeField] private float scaleAmount = 0.03f;
    [SerializeField] private float scaleSpeed = 1.5f;

    private Vector2 startPos;
    private Vector2 homePosition;
    private Vector2 targetPos;
    private float facing = 1f;

    private float moveDuration = 2f;
    private float moveTimer = 0f;

    void Start()
    {
        targetPos = fishTransform.anchoredPosition;
        homePosition = fishTransform.anchoredPosition;
        PickNewTarget();
    }

    void Update()
    {
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
    }

    void PickNewTarget()
    {
        startPos = fishTransform.anchoredPosition;
        targetPos = homePosition + Random.insideUnitCircle * moveRadius;
        moveTimer = 0f;
    }

    public void UpdateFish(FishData fish)
    {
        artwork.sprite = fish.fishSprite;
        fishName.text = fish.fishName;
    }
}