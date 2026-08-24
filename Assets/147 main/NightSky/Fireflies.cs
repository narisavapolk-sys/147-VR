using UnityEngine;

/// <summary>
/// Fireflies
/// -----------------------------------------------------------------------
/// Fireflies drifting near the floor (small yellow/green light points, slowly drifting and gently flickering).
/// Uses the same Particle System pattern as DriftingConcertSmoke.cs.
///
/// Usage:
/// 1. Create an Empty GameObject named "Fireflies" and place it in the desired area.
/// 2. Attach this script.
/// 3. Press Play — fireflies will appear, drift, and flicker immediately, no extra setup needed.
/// -----------------------------------------------------------------------
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class Fireflies : MonoBehaviour
{
    [Header("Firefly drift area (meters)")]
    public Vector3 areaSize = new Vector3(10f, 2f, 10f);

    [Header("Number of fireflies")]
    [Range(5, 100)] public int fireflyCount = 35;

    [Header("Drift speed")]
    [Range(0.05f, 1f)] public float driftSpeed = 0.25f;

    [Header("Light point size (meters)")]
    public Vector2 sizeRange = new Vector2(0.04f, 0.09f);

    [Header("Firefly color")]
    public Color fireflyColor = new Color(0.85f, 1f, 0.45f);

    [Header("Flicker speed")]
    [Range(0.5f, 4f)] public float flickerSpeed = 1.8f;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();
        Configure();
    }

    void Configure()
    {
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 999f; // live indefinitely, never disappear mid-air
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = fireflyColor;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = fireflyCount + 5;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, (short)fireflyCount)
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;

        // Slow random drift in all directions (noise) instead of straight-line flight
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = driftSpeed;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // Gentle flicker via color-over-lifetime loop (long lifetime above helps smooth the pattern)
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(fireflyColor, 0f), new GradientColorKey(fireflyColor, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.2f, 0f), new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.3f, 0.35f), new GradientAlphaKey(1f, 0.55f),
                new GradientAlphaKey(0.25f, 0.75f), new GradientAlphaKey(0.9f, 0.9f),
                new GradientAlphaKey(0.2f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        if (psRenderer != null)
        {
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (mat.shader == null || !mat.shader.isSupported)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            }
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 1);   // Additive — for a realistic glowing firefly look
            psRenderer.material = mat;
        }
    }
}
