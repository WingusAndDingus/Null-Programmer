using UnityEngine;

public class blinking : MonoBehaviour
{
    [Header("Alpha Settings")]
    [Range(0f, 1f)] public float maxAlpha = 1.0f;
    [Tooltip("Pulsing speed")]
    public float speed = 2.0f;

    private SpriteRenderer sr;
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (sr != null)
        {
            float t = (Mathf.Sin(Time.time * speed) + 1f) / 2f;
            float currentAlpha = Mathf.Lerp(0f, maxAlpha, t);

            Color color = sr.color;
            color.a = currentAlpha;
            sr.color = color;
        }
    }
}
