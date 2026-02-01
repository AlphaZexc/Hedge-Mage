using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraFollow : MonoBehaviour
{
    [Header("Screen Shake")]
    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 0.3f;
    public AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0,1,1,0);

    private float shakeTimer = 0f;
    private float shakeStrength = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    public void Shake(float magnitude = -1f, float duration = -1f)
    {
        shakeStrength = (magnitude > 0f) ? magnitude : shakeMagnitude;
        shakeTimer = (duration > 0f) ? duration : shakeDuration;
    }
    public Transform target;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Zoom Settings")]
    public float mazeZoom = 5f;
    public float courtyardZoom = 8f;
    public float zoomSmoothSpeed = 2f;

    [Header("Light Settings")]
    public Light2D mainLight2D;
    public float mazeLightRadius = 5f;
    public float courtyardLightRadius = 10f;
    public float lightSmoothSpeed = 2f;

    private Camera cam;
    private bool inCourtyard = false;

    void Start()
    {
        cam = Camera.main;
        if (mainLight2D == null)
            mainLight2D = FindFirstObjectByType<Light2D>();

        // Assume player starts in courtyard
        inCourtyard = true;
        if (cam != null)
            cam.orthographicSize = courtyardZoom;
        if (mainLight2D != null)
            mainLight2D.pointLightOuterRadius = courtyardLightRadius;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Apply screen shake
        if (shakeTimer > 0f)
        {
            float shakeProgress = 1f - (shakeTimer / shakeDuration);
            float curveStrength = shakeCurve.Evaluate(shakeProgress);
            shakeOffset = Random.insideUnitCircle * shakeStrength * curveStrength;
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        transform.position = smoothedPosition + shakeOffset;

        // Smooth zoom
        float targetZoom = inCourtyard ? courtyardZoom : mazeZoom;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSmoothSpeed);

        // Smooth light radius
        if (mainLight2D != null)
        {
            float targetRadius = inCourtyard ? courtyardLightRadius : mazeLightRadius;
            mainLight2D.pointLightOuterRadius = Mathf.Lerp(mainLight2D.pointLightOuterRadius, targetRadius, Time.deltaTime * lightSmoothSpeed);
        }
    }

    // Called by trigger script
    public void SetCourtyardState(bool isInCourtyard)
    {
        inCourtyard = isInCourtyard;
    }
}

