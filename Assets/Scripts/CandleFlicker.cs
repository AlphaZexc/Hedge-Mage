using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlickeringLight2D : MonoBehaviour
{
    [Header("Snuff/Relight Effect Settings")]
    [Tooltip("How long the candle takes to relight after being snuffed (seconds)")]
    public float relightEffectDuration = 1.2f;
    [Tooltip("How fast the flicker is at the start of relight (lower = faster)")]
    public float relightFlickerSpeed = 0.05f;
    public float minIntensity = 0.8f;
    public float maxIntensity = 1.2f;
    public float flickerSpeed = 0.1f;

    private Light2D light2D;
    private float targetIntensity;
    private float flickerTimer;

    private float defaultMinIntensity;
    private float defaultMaxIntensity;
    private float defaultFlickerSpeed;
    private float relightTimer = 0f;
    private float relightDuration = 1.2f;
    private bool isRelighting = false;

    void Awake()
    {
        defaultMinIntensity = minIntensity;
        defaultMaxIntensity = maxIntensity;
        defaultFlickerSpeed = flickerSpeed;
    }

    void Start()
    {
        light2D = GetComponent<Light2D>();
        if (light2D == null)
        {
            Debug.LogWarning("FlickeringLight2D: No Light2D component found.");
            enabled = false;
        }
        targetIntensity = light2D.intensity;
    }

    void Update()
    {
        if (isRelighting)
        {
            relightTimer += Time.deltaTime;
            float t = Mathf.Clamp01(relightTimer / relightDuration);
            // Fast flicker and fade in
            minIntensity = 0f;
            maxIntensity = Mathf.Lerp(0f, defaultMaxIntensity, t);
            flickerSpeed = Mathf.Lerp(relightFlickerSpeed, defaultFlickerSpeed, t); // Fast at first, slows to normal
            if (t >= 1f)
            {
                isRelighting = false;
                minIntensity = defaultMinIntensity;
                maxIntensity = defaultMaxIntensity;
                flickerSpeed = defaultFlickerSpeed;
            }
        }

        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
            flickerTimer = flickerSpeed;
        }
        light2D.intensity = Mathf.Lerp(light2D.intensity, targetIntensity, Time.deltaTime * 10f);
    }

    // Call this to snuff and relight the candle
    public void SnuffAndRelight(float duration = 1.2f)
    {
        relightDuration = (duration > 0f) ? duration : relightEffectDuration;
        relightTimer = 0f;
        isRelighting = true;
        minIntensity = 0f;
        maxIntensity = 0f;
        flickerSpeed = relightFlickerSpeed;
        targetIntensity = 0f;
        if (light2D != null) light2D.intensity = 0f;
    }
}

