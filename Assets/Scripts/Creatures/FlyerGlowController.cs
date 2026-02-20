using UnityEngine;

/// <summary>
/// Manages the animated glow effect on the Flyer's SpriteRenderer.
///
/// SHADER GRAPH REQUIREMENTS — you MUST fix these in the Shader Graph editor for this to work:
///
///   1. CONNECT EMISSION:
///      The Multiply node output (Glow Mask × Color) → connect its Out(4) port
///      to the Fragment node's Emission(3) port.
///      Without this the glow is never submitted to the URP Bloom post-process.
///
///   2. MARK COLOR AS HDR:
///      Select the "Color" property in the Shader Graph Blackboard.
///      In the Node Settings panel check "HDR".
///      The color must have intensity > 1.0 to trigger Bloom.
///
///   3. MATCH SECONDARY TEXTURE NAME:
///      In your sprite sheet's Sprite Editor → Secondary Textures,
///      the Name field must exactly match the Shader Graph property reference name.
///      The Shader Graph property "Glow Mask" has a default reference of "_GlowMask".
///      Open the Blackboard, select "Glow Mask", check its Reference field — set the
///      Sprite Editor secondary texture name to that exact same value (e.g. _GlowMask).
///      Unity will then automatically pass the correct atlas UV so the mask animates
///      in sync with the main sprite — no code needed for UV sync.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FlyerGlowController : MonoBehaviour
{
    [Header("Glow Settings")]
    [Tooltip("Base HDR color of the glow. Must be HDR (intensity > 1) to trigger Bloom.")]
    [ColorUsage(true, true)]
    public Color glowColor = new Color(0.3f, 1.0f, 0.9f, 1f); // cyan, intensity ~1.5

    [Tooltip("Minimum glow intensity multiplier for pulsing.")]
    [Range(0f, 2f)] public float pulseMin = 0.7f;

    [Tooltip("Maximum glow intensity multiplier for pulsing.")]
    [Range(0f, 4f)] public float pulseMax = 1.5f;

    [Tooltip("Speed of the glow pulse in cycles per second.")]
    [Range(0.1f, 5f)] public float pulseSpeed = 1.2f;

    [Tooltip("If false the glow stays at a constant intensity (no pulsing).")]
    public bool enablePulse = true;

    // ── Shader property IDs (matches the Shader Graph property reference names) ──
    // If you renamed "Color" or "Glow Mask" in the Shader Graph Blackboard,
    // update these strings to match the Reference name shown there.
    private static readonly int ColorPropID   = Shader.PropertyToID("_Color");
    private static readonly int GlowMaskPropID = Shader.PropertyToID("_GlowMask");

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock mpb;

    // ── Validation ──────────────────────────────────────────────────────────────

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (spriteRenderer.sharedMaterial == null)
        {
            Debug.LogError("[FlyerGlow] No material assigned to the SpriteRenderer.", gameObject);
            enabled = false;
            return;
        }

        // Warn if the shader doesn't expose a Color or GlowMask property —
        // this usually means the reference names don't match what's in the Shader Graph.
        Material mat = spriteRenderer.sharedMaterial;
        if (!mat.HasProperty(ColorPropID))
            Debug.LogWarning("[FlyerGlow] Material has no '_Color' property. " +
                "Check the 'Color' property Reference name in the Shader Graph Blackboard.", gameObject);

        if (!mat.HasProperty(GlowMaskPropID))
            Debug.LogWarning("[FlyerGlow] Material has no '_GlowMask' property. " +
                "Check the 'Glow Mask' property Reference name in the Shader Graph Blackboard " +
                "AND in the Sprite Editor → Secondary Textures.", gameObject);
    }

    // ── Runtime update ──────────────────────────────────────────────────────────

    void LateUpdate()
    {
        // Calculate pulsed intensity multiplier
        float pulse = 1f;
        if (enablePulse)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f; // 0..1
            pulse = Mathf.Lerp(pulseMin, pulseMax, t);
        }

        // Build pulsed HDR color — glowColor already carries the base HDR intensity,
        // multiplying by pulse makes it breathe brighter/dimmer around that base.
        Color pulsedColor = glowColor * pulse;

        // Push to renderer without creating a new material instance (important for batching)
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(ColorPropID, pulsedColor);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    // ── Public API ──────────────────────────────────────────────────────────────

    /// <summary>Instantly set a new HDR glow color at runtime (e.g. when carrying a letter).</summary>
    public void SetGlowColor(Color hdrColor)
    {
        glowColor = hdrColor;
    }

    /// <summary>Enable or disable the glow entirely.</summary>
    public void SetGlowActive(bool active)
    {
        enabled = active;
        if (!active)
        {
            // Push a fully transparent / black color so the glow disappears cleanly
            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(ColorPropID, Color.black);
            spriteRenderer.SetPropertyBlock(mpb);
        }
    }
}
