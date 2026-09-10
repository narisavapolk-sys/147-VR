using UnityEngine;

/// <summary>
/// TwinklingStars
/// -----------------------------------------------------------------------
/// Bright stars that twinkle gently at random intervals (tiny background stars baked in the HDRI don't twinkle
/// because real distant stars rarely flicker to the eye — this simulates nearby/bright stars only).
///
/// Usage:
/// 1. Create an Empty GameObject named "TwinklingStarsManager"
/// 2. Attach this script and assign a Star Sprite (soft glowing white circle with transparent background) to the Star Sprite slot.
/// 3. Press Play — stars will spread across a dome around the player and twinkle automatically.
/// -----------------------------------------------------------------------
/// </summary>
public class TwinklingStars : MonoBehaviour
{
    [Header("Number of twinkling stars")]
    [Range(5, 80)] public int starCount = 30;

    [Header("Star sprite (soft glowing white circle, transparent background)")]
    public Sprite starSprite;

    [Header("Distance from player")]
    public float sphereRadius = 380f;

    [Header("Star size (world units)")]
    public Vector2 sizeRange = new Vector2(0.8f, 2.2f);

    [Header("Twinkle speed (cycles/second)")]
    public Vector2 twinkleSpeedRange = new Vector2(0.3f, 1.2f);

    private Transform[] stars;
    private float[] phase;
    private float[] speed;
    private float[] baseAlpha;
    private SpriteRenderer[] renderers;

    void Start()
    {
        Transform player = Camera.main != null ? Camera.main.transform : transform;
        stars = new Transform[starCount];
        phase = new float[starCount];
        speed = new float[starCount];
        baseAlpha = new float[starCount];
        renderers = new SpriteRenderer[starCount];

        for (int i = 0; i < starCount; i++)
        {
            GameObject go = new GameObject($"TwinkleStar_{i}");
            go.transform.SetParent(transform);

            float theta = Random.Range(0f, Mathf.PI * 0.5f);
            float phi = Random.Range(0f, Mathf.PI * 2f);
            Vector3 dir = new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta)*Mathf.Sin(phi));
            go.transform.position = player.position + dir * sphereRadius;

            float size = Random.Range(sizeRange.x, sizeRange.y);
            go.transform.localScale = Vector3.one * size;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = starSprite;
            sr.color = new Color(1f, 0.98f, 0.92f, 1f);

            stars[i] = go.transform;
            renderers[i] = sr;
            phase[i] = Random.Range(0f, Mathf.PI * 2f);
            speed[i] = Random.Range(twinkleSpeedRange.x, twinkleSpeedRange.y);
            baseAlpha[i] = Random.Range(0.5f, 1f);
        }
    }

    void Update()
    {
        if (renderers == null) return;
        float t = Time.time;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            float flicker = (Mathf.Sin(t * speed[i] * Mathf.PI * 2f + phase[i]) + 1f) * 0.5f;
            float alpha = baseAlpha[i] * Mathf.Lerp(0.35f, 1f, flicker);
            Color c = renderers[i].color;
            c.a = alpha;
            renderers[i].color = c;

            // Billboard: always face the player
            if (Camera.main != null)
                stars[i].rotation = Camera.main.transform.rotation;
        }
    }
}
