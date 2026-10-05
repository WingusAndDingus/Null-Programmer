using System.Collections;
using UnityEngine;

public class WakeUpAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup eyelids;

    [Header("Initial Wake Up")]
    [SerializeField] private float initialFadeDuration = 1.2f;

    [Header("Blink Settings")]
    [SerializeField] private int numberOfBlinks = 2;
    [SerializeField] private float blinkCloseDuration = 0.12f;
    [SerializeField] private float blinkOpenDuration = 0.2f;
    [SerializeField] private float timeBetweenBlinks = 0.5f;

    private void Start()
    {
        StartCoroutine(WakeUp());
    }

    private IEnumerator WakeUp()
    {
        // Start completely dark
        eyelids.alpha = 1f;

        // Slowly open eyes
        yield return FadeEyes(1f, 0f, initialFadeDuration);

        // Do the requested number of blinks
        for (int i = 0; i < numberOfBlinks; i++)
        {
            // Close eyes
            yield return FadeEyes(0f, 1f, blinkCloseDuration);

            // Briefly keep eyes closed
            yield return new WaitForSeconds(0.08f);

            // Open eyes
            yield return FadeEyes(1f, 0f, blinkOpenDuration);

            // Wait before the next blink
            if (i < numberOfBlinks - 1)
            {
                yield return new WaitForSeconds(timeBetweenBlinks);
            }
        }
    }

    private IEnumerator FadeEyes(float start, float end, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;
            eyelids.alpha = Mathf.Lerp(start, end, t);

            yield return null;
        }

        eyelids.alpha = end;
    }
}
