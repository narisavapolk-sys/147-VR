using UnityEngine;
using System.Collections;

/// <summary>
/// ShootingStars
/// -----------------------------------------------------------------------
/// Shooting stars streak across the sky periodically (average every ~10 seconds with slight randomness for realism).
///
/// Usage:
/// 1. Create an Empty GameObject named "ShootingStarsManager"
/// 2. Attach this script.
/// 3. Press Play — shooting stars will streak across the sky automatically, no extra setup needed.
/// -----------------------------------------------------------------------
/// </summary>
public class ShootingStars : MonoBehaviour
{
    [Header("Interval (seconds) — average about 10 seconds")]
    public float minInterval = 7f;
    public float maxInterval = 13f;

    [Header("Shooting star speed (units/second)")]
    public float streakSpeed = 60f;

    [Header("Trail length")]
    public float trailLength = 8f;

    [Header("Distance from player (should be beyond normal view range to simulate sky)")]
    public float sphereRadius = 400f;

    [Header("Shooting star color")]
    public Color streakColor = new Color(1f, 0.97f, 0.9f);

    private Transform player;

    void Start()
    {
        if (Camera.main != null) player = Camera.main.transform;
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);
            SpawnShootingStar();
        }
    }

    void SpawnShootingStar()
    {
        Vector3 center = player != null ? player.position : Vector3.zero;

        // Random start point on the upper half of the sky sphere
        float theta = Random.Range(0f, Mathf.PI * 0.35f); // tilted upward
        float phi = Random.Range(0f, Mathf.PI * 2f);
        Vector3 startDir = new Vector3(
            Mathf.Sin(theta) * Mathf.Cos(phi),
            Mathf.Cos(theta),
            Mathf.Sin(theta) * Mathf.Sin(phi)
        );
        Vector3 startPos = center + startDir * sphereRadius;

        // Travel direction: random diagonal down/sideways for a realistic look
        Vector3 travelDir = (Quaternion.Euler(
            Random.Range(-25f, 25f), Random.Range(0f, 360f), 0f
        ) * Vector3.down).normalized;

        GameObject star = new GameObject("ShootingStar_Instance");
        TrailRenderer trail = star.AddComponent<TrailRenderer>();
        trail.time = trailLength / streakSpeed;
        trail.startWidth = 1.2f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(streakColor, 0f), new GradientColorKey(streakColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trail.colorGradient = grad;
        trail.minVertexDistance = 0.5f;

        star.transform.position = startPos;
        StartCoroutine(MoveStar(star, travelDir));
    }

    IEnumerator MoveStar(GameObject star, Vector3 dir)
    {
        float traveled = 0f;
        float maxTravel = sphereRadius * 0.7f;
        while (star != null && traveled < maxTravel)
        {
            float step = streakSpeed * Time.deltaTime;
            star.transform.position += dir * step;
            traveled += step;
            yield return null;
        }
        if (star != null)
        {
            Destroy(star, 1.5f); // let the trail fade before destroying
        }
    }
}
