using UnityEngine;

/// <summary>
/// Scene singleton that exposes a shared ambient wind direction/strength
/// for foliage sway, leaf particles, and hedge shaders.
/// </summary>
public class AmbientWind : MonoBehaviour
{
    public static AmbientWind Instance { get; private set; }

    [Tooltip("World-space wind direction (normalized on Awake).")]
    public Vector2 direction = new Vector2(1f, 0.2f);

    [Tooltip("Base wind strength. 1 = default mild breeze.")]
    public float strength = 1f;

    [Header("Strength Noise")]
    public bool useStrengthNoise = true;
    [Range(0f, 1f)]
    public float noiseAmplitude = 0.25f;
    public float noiseSpeed = 0.35f;

    static readonly int WindDirId = Shader.PropertyToID("_AmbientWindDir");
    static readonly int WindStrengthId = Shader.PropertyToID("_AmbientWindStrength");

    float _noiseSeed;

    public Vector2 Direction
    {
        get
        {
            if (direction.sqrMagnitude < 0.0001f)
                return Vector2.right;
            return direction.normalized;
        }
    }

    public float Strength
    {
        get
        {
            if (!useStrengthNoise)
                return strength;

            float n = Mathf.PerlinNoise(_noiseSeed, Time.time * noiseSpeed);
            return strength * (1f + (n - 0.5f) * 2f * noiseAmplitude);
        }
    }

    public Vector2 Velocity => Direction * Strength;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _noiseSeed = Random.Range(0f, 100f);
        if (direction.sqrMagnitude > 0.0001f)
            direction = direction.normalized;

        PushShaderGlobals();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LateUpdate()
    {
        PushShaderGlobals();
    }

    public void PushShaderGlobals()
    {
        Vector2 dir = Direction;
        Shader.SetGlobalVector(WindDirId, new Vector4(dir.x, dir.y, 0f, 0f));
        Shader.SetGlobalFloat(WindStrengthId, Strength);
    }

    public static Vector2 GetVelocityOrDefault()
    {
        return Instance != null ? Instance.Velocity : new Vector2(1f, 0.2f).normalized;
    }

    public static float GetStrengthOrDefault()
    {
        return Instance != null ? Instance.Strength : 1f;
    }

    public static Vector2 GetDirectionOrDefault()
    {
        return Instance != null ? Instance.Direction : new Vector2(1f, 0.2f).normalized;
    }
}
