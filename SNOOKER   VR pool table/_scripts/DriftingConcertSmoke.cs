using UnityEngine;

/// <summary>
/// DriftingConcertSmoke
/// -----------------------------------------------------------------------
/// Ambient drifting concert smoke — no manual Particle System setup needed.
///
/// Usage:
/// 1. Create an Empty GameObject in the scene (e.g. named "ConcertSmoke") and place it near floor level in the center of the room.
/// 2. Attach this script to that GameObject.
/// 3. Assign the SmokeParticle_Texture.png to the "Smoke Texture" slot in the Inspector.
/// 4. Press Play — smoke will appear and drift immediately, no extra setup needed.
///
/// Tune parameters below in the Inspector (density, drift speed, color, size).
/// -----------------------------------------------------------------------
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class DriftingConcertSmoke : MonoBehaviour
{
    [Header("Required: assign SmokeParticle_Texture.png here")]
    public Texture2D smokeTexture;

    [Header("Smoke spread area (meters)")]
    public Vector3 areaSize = new Vector3(14f, 0.5f, 14f);

    [Header("Smoke density (particles per second)")]
    [Range(0.2f, 10f)] public float emissionRate = 2.5f;

    [Header("Drift speed upward (meters/second)")]
    [Range(0.02f, 1f)] public float driftSpeed = 0.15f;

    [Header("Smoke particle size (meters)")]
    public Vector2 sizeRange = new Vector2(3f, 7f);

    [Header("Smoke particle lifetime (seconds) — longer = drifts further")]
    public Vector2 lifetimeRange = new Vector2(18f, 30f);

    [Header("Smoke opacity (0=invisible, 1=opaque)")]
    [Range(0.02f, 0.5f)] public float maxOpacity = 0.12f;

    [Header("Smoke color (cool concert tone or warm white)")]
    public Color smokeColor = new Color(0.75f, 0.78f, 0.85f, 1f);

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
        // ----- Main module -----
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(driftSpeed * 0.5f, driftSpeed * 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = new Color(smokeColor.r, smokeColor.g, smokeColor.b, maxOpacity);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = 0f;

        // ----- Emission -----
        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        // ----- Shape: spread across the room floor area -----
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;

        // ----- Velocity over lifetime: slow upward drift + gentle sway for realism -----
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(driftSpeed);
        vel.x = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.3f, driftSpeed * 0.3f);
        vel.z = new ParticleSystem.MinMaxCurve(-driftSpeed * 0.3f, driftSpeed * 0.3f);

        // ----- Noise: make smoke diffuse and non-linear, like real air currents -----
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.15f;
        noise.scrollSpeed = 0.1f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // ----- Size over lifetime: expand as smoke drifts (natural smoke expansion) -----
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.6f);
        sizeCurve.AddKey(0.5f, 1.0f);
        sizeCurve.AddKey(1f, 1.6f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ----- Color over lifetime: fade in on spawn, fade out before disappearing -----
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(smokeColor, 0f),
                new GradientColorKey(smokeColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // ----- Rotation over lifetime: slow spin for realism, not a static billboard -----
        var rotOverLifetime = ps.rotationOverLifetime;
        rotOverLifetime.enabled = true;
        rotOverLifetime.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

        // ----- Renderer: soft additive/alpha blend material -----
        if (psRenderer != null)
        {
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.alignment = ParticleSystemRenderSpace.View;

            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            if (mat.shader == null || !mat.shader.isSupported)
            {
                // Fallback for URP/HDRP — use the standard particle shader if available
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            }
            if (smokeTexture != null)
            {
                mat.SetTexture("_BaseMap", smokeTexture);
                mat.SetTexture("_MainTex", smokeTexture);
            }
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);   // Alpha blend (not additive, for natural smoke appearance)
            psRenderer.material = mat;
        }
    }
}
