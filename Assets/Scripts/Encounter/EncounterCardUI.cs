using System.Collections;
using TMPro;
using UnityEngine;

public class EncounterCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text title;

    [Header("Card Faces")]
    [SerializeField] private GameObject front;
    [SerializeField] private GameObject back;

    [Header("Slide")]
    [SerializeField] private float slideDuration = 0.35f;

    [Header("Flip")]
    [SerializeField] private float flipDuration = 0.18f;

    [Header("Polish")]
    [SerializeField] private float settleRotation = 6f;
    [SerializeField] private AnimationCurve slideCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    public Vector2 TargetPosition { get; set; }
    public bool IsFinished { get; private set; }

    private RectTransform rect;

    public void Initialize(EncounterCardData card, RectTransform spawnPoint)
    {
        IsFinished = false;

        rect = GetComponent<RectTransform>();

        title.text =
            card.fishData != null
            ? card.fishData.fishName
            : card.category.ToString();

        rect.anchoredPosition = spawnPoint.anchoredPosition;

        rect.localScale = Vector3.one;

        rect.localRotation =
            Quaternion.Euler(0f, 0f, -settleRotation);

        front.SetActive(false);
        back.SetActive(true);
    }

    public void StartReveal()
    {
        StopAllCoroutines();
        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        Vector2 startPos = rect.anchoredPosition;

        float t = 0f;

        // ---------- Slide ----------
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;

            float eased = slideCurve.Evaluate(t);

            rect.anchoredPosition =
                Vector2.Lerp(startPos, TargetPosition, eased);

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(-settleRotation, 0f, eased));

            yield return null;
        }

        rect.anchoredPosition = TargetPosition;
        rect.localRotation = Quaternion.identity;

        yield return new WaitForSeconds(0.04f);

        // ---------- Flip ----------
        t = 0f;
        bool switched = false;

        while (t < 1f)
        {
            t += Time.deltaTime / flipDuration;

            float angle = Mathf.Lerp(180f, 0f, t);

            float width =
                Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));

            rect.localScale =
                new Vector3(width, 1f, 1f);

            if (!switched && angle <= 90f)
            {
                switched = true;

                back.SetActive(false);
                front.SetActive(true);
            }

            yield return null;
        }

        // Tiny pop
        rect.localScale = Vector3.one * 1.06f;

        yield return new WaitForSeconds(0.05f);

        IsFinished = true;
        rect.localScale = Vector3.one;
    }
}