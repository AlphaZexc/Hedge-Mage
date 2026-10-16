using UnityEngine;

/// <summary>
/// Gentle canopy sway driven by AmbientWind. Attach to a visual transform only
/// (not the physics root) so colliders stay stable.
/// </summary>
public class FoliageSway : MonoBehaviour
{
    [Tooltip("Peak local Z rotation in degrees at strength 1.")]
    public float angleAmplitude = 2.2f;

    [Tooltip("Peak local X offset at strength 1.")]
    public float positionAmplitude = 0.012f;

    public float speed = 1.15f;

    [Tooltip("Randomize phase so nearby trees do not sway in sync.")]
    public bool randomizePhase = true;

    float _phase;
    Vector3 _baseLocalPos;
    Quaternion _baseLocalRot;

    void Awake()
    {
        _baseLocalPos = transform.localPosition;
        _baseLocalRot = transform.localRotation;
        _phase = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void Update()
    {
        Vector2 wind = AmbientWind.GetVelocityOrDefault();
        float str = AmbientWind.GetStrengthOrDefault();
        float t = Time.time * speed + _phase;

        float sway = Mathf.Sin(t) * angleAmplitude * str;
        float windBias = wind.x * 0.4f * angleAmplitude * str;
        transform.localRotation = _baseLocalRot * Quaternion.Euler(0f, 0f, sway + windBias);

        float sign = wind.x < 0f ? -1f : 1f;
        float xOff = Mathf.Sin(t) * positionAmplitude * str * sign;
        transform.localPosition = _baseLocalPos + new Vector3(xOff, 0f, 0f);
    }
}
