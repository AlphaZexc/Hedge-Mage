using UnityEngine;

/// <summary>
/// Configures a low-rate leaf ParticleSystem and pushes wind force from AmbientWind.
/// Safe to place on an empty child; creates the ParticleSystem at runtime if missing.
/// </summary>
[DisallowMultipleComponent]
public class LeafFallWind : MonoBehaviour
{
    [SerializeField] Sprite leafSprite;
    [SerializeField] float emissionRate = 1.15f;
    [SerializeField] float lifetime = 1.35f;
    [SerializeField] float startSize = 0.035f;
    [SerializeField] float gravityModifier = 0.1f;
    [SerializeField] float windForceScale = 0.18f;
    [SerializeField] float startSpeedMin = 0.02f;
    [SerializeField] float startSpeedMax = 0.07f;
    [SerializeField] int sortingOrderOffset = 2;
    // Dominant canopy pink from free_bundle_2_trees_0 (~196,135,240).
    [SerializeField] Color leafTint = new Color(0.769f, 0.529f, 0.941f, 1f);
    [SerializeField] Color leafTintEnd = new Color(0.51f, 0.35f, 0.81f, 1f);

    ParticleSystem _ps;
    ParticleSystem.ForceOverLifetimeModule _force;
    ParticleSystemRenderer _renderer;
    bool _configured;

    void Awake()
    {
        EnsureParticleSystem();
        Configure();
    }

    void LateUpdate()
    {
        if (!_configured || _ps == null)
            return;

        Vector2 wind = AmbientWind.GetVelocityOrDefault();
        var force = _force;
        force.x = wind.x * windForceScale;
        force.y = wind.y * windForceScale * 0.25f - 0.02f;
        _force = force;

        // Keep leaves just above the canopy sort order when YSort updates the parent.
        var parentRenderer = transform.parent != null
            ? transform.parent.GetComponent<SpriteRenderer>()
            : null;
        if (parentRenderer != null && _renderer != null)
        {
            _renderer.sortingLayerID = parentRenderer.sortingLayerID;
            _renderer.sortingOrder = parentRenderer.sortingOrder + sortingOrderOffset;
        }
    }

    void EnsureParticleSystem()
    {
        _ps = GetComponent<ParticleSystem>();
        if (_ps == null)
            _ps = gameObject.AddComponent<ParticleSystem>();

        _renderer = GetComponent<ParticleSystemRenderer>();
        if (_renderer == null)
            _renderer = gameObject.AddComponent<ParticleSystemRenderer>();
    }

    void Configure()
    {
        if (_ps == null)
            return;

        var main = _ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeedMin, startSpeedMax);
        main.startSize = startSize;
        main.startColor = leafTint;
        main.gravityModifier = gravityModifier;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = _ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        var shape = _ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;
        shape.radiusThickness = 1f;

        var colorOverLifetime = _ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(leafTint, 0f),
                new GradientColorKey(leafTintEnd, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.9f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        var rotation = _ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        _force = _ps.forceOverLifetime;
        _force.enabled = true;
        _force.space = ParticleSystemSimulationSpace.World;

        var noise = _ps.noise;
        noise.enabled = true;
        noise.strength = 0.035f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.15f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        if (_renderer != null)
        {
            // Dynamic sorting layer (uniqueID 3178462243 as signed int).
            _renderer.sortingLayerID = -1116505053;
            _renderer.sortingOrder = 14;
            _renderer.renderMode = ParticleSystemRenderMode.Billboard;
            _renderer.minParticleSize = 0f;
            _renderer.maxParticleSize = 0.12f;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
                _renderer.material = new Material(shader);
        }

        if (leafSprite != null)
        {
            var tsa = _ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;
            while (tsa.spriteCount > 0)
                tsa.RemoveSprite(0);
            tsa.AddSprite(leafSprite);
        }

        _configured = true;
        if (!_ps.isPlaying)
            _ps.Play();
    }
}
