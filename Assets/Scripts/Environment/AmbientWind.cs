using UnityEngine;

/// <summary>
/// Scene singleton for shared ambient wind used by leaf-fall particles.
/// Holds a direction that occasionally reverses left↔right like shifting gusts.
/// </summary>
public class AmbientWind : MonoBehaviour
{
    public static AmbientWind Instance { get; private set; }

    [Tooltip("Base wind direction before left/right flips (normalized on Awake).")]
    public Vector2 direction = new Vector2(1f, 0.15f);

    [Tooltip("Base wind strength. 1 = default mild breeze.")]
    public float strength = 1f;

    [Header("Strength Noise")]
    public bool useStrengthNoise = true;
    [Range(0f, 1f)]
    public float noiseAmplitude = 0.2f;
    public float noiseSpeed = 0.35f;

    [Header("Direction Reversal")]
    [Tooltip("Occasionally flip horizontal wind left↔right.")]
    public bool reverseDirection = true;
    public float minHoldSeconds = 3.5f;
    public float maxHoldSeconds = 8f;
    public float flipBlendSeconds = 1.25f;

    Vector2 _baseDirection;
    float _noiseSeed;
    float _sign = 1f;
    float _fromSign = 1f;
    float _toSign = 1f;
    float _holdRemaining;
    float _blendT = 1f;

    public Vector2 Direction
    {
        get
        {
            if (_baseDirection.sqrMagnitude < 0.0001f)
                return new Vector2(_sign, 0f);

            return new Vector2(_baseDirection.x * _sign, _baseDirection.y).normalized;
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
        _baseDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right;
        // Keep inspector field readable as the base (positive) direction.
        direction = _baseDirection;

        _sign = Random.value < 0.5f ? -1f : 1f;
        _fromSign = _sign;
        _toSign = _sign;
        _blendT = 1f;
        _holdRemaining = Random.Range(minHoldSeconds, maxHoldSeconds);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!reverseDirection)
        {
            _sign = 1f;
            return;
        }

        if (_blendT < 1f)
        {
            float duration = Mathf.Max(0.05f, flipBlendSeconds);
            _blendT = Mathf.Min(1f, _blendT + Time.deltaTime / duration);
            float t = Mathf.SmoothStep(0f, 1f, _blendT);
            _sign = Mathf.Lerp(_fromSign, _toSign, t);
            return;
        }

        _holdRemaining -= Time.deltaTime;
        if (_holdRemaining > 0f)
            return;

        _fromSign = _toSign;
        _toSign = -_toSign;
        _blendT = 0f;
        _holdRemaining = Random.Range(minHoldSeconds, maxHoldSeconds);
    }

    public static Vector2 GetVelocityOrDefault()
    {
        return Instance != null ? Instance.Velocity : new Vector2(1f, 0.15f).normalized;
    }

    public static float GetStrengthOrDefault()
    {
        return Instance != null ? Instance.Strength : 1f;
    }

    public static Vector2 GetDirectionOrDefault()
    {
        return Instance != null ? Instance.Direction : new Vector2(1f, 0.15f).normalized;
    }
}
