using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Flyer : MonoBehaviour
{
    [Header("General")]
    public float cooldown = 60f;
    public GameObject carriedLetterVisual; // Assign a child GameObject to show the letter

    [Header("Movement")]
    public float flyOverDuration = 2f;
    public float circleDistance = 4f;
    public float circleBetweenSwoopsDuration = 1.5f;
    public float minDropDistanceFromPlayer = 5f;
    public float maxDropDistanceFromPlayer = 20f;
    public int maxSwoopAttempts = 3;
    [Range(0f, 1f)] public float stealSuccessPercent = 0.3f;
    [SerializeField] private float circleAngularSpeed = 2f;
    [SerializeField] private float approachSpeed = 8f;
    [SerializeField] private float spawnVisionPadding = 1.5f;

    [Header("Swoop")]
    [SerializeField] private float swoopTime = 0.9f;
    [SerializeField] private float swoopDipDepth = 1.1f;
    [SerializeField] private float letterFlightDuration = 0.32f;

    [Header("Audio")]
    [SerializeField] private AudioClip swoopWhooshClip;
    [SerializeField] private AudioClip stealClip;
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 0.85f;

    private Animator animator;
    private FlyerGlowController glowController;
    private Rigidbody2D rb;

    private Collider2D flyerCollider;
    private Transform player;
    private PlayerInventory playerInventory => PlayerInventory.Instance;
    private LetterObject carriedLetterObject;
    private int swoopAttempts = 0;
    private Vector3 dropPosition;
    private bool hasStolenLetter = false;
    private Coroutine letterFlightRoutine;

    /// <summary>Polar angle on the circle around the player (radians).</summary>
    private float circleAngle;
    private Vector3 lastFacingDir = Vector3.down;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        carriedLetterVisual.SetActive(false);
        flyerCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        var visuals = transform.Find("Visuals");
        if (visuals != null)
        {
            animator = visuals.GetComponent<Animator>();
            glowController = visuals.GetComponent<FlyerGlowController>();
            if (glowController == null)
                Debug.LogWarning("[Flyer] 'Visuals' child is missing a FlyerGlowController component. " +
                    "Add FlyerGlowController to the Visuals child to enable the animated glow.");
        }
        else
            Debug.LogWarning("[Flyer] Could not find 'Visuals' child for Animator/Glow reference.");

        RelocateOutsidePlayerVision();
        StartCoroutine(FlyerRoutine());
    }

    private IEnumerator FlyerRoutine()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();

        // Fly in to the circle perimeter, then begin circling (not appear mid-orbit)
        yield return ApproachCirclePerimeter();
        yield return CirclePlayerForDuration(flyOverDuration);

        while (swoopAttempts < maxSwoopAttempts && !hasStolenLetter)
        {
            if (playerHealth.isDead)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return SwoopAttempt();
            swoopAttempts++;

            // Resume circling from the exact opposite-perimeter angle (no Atan2 resync snap)
            if (!hasStolenLetter && swoopAttempts < maxSwoopAttempts)
                yield return CirclePlayerForDuration(circleBetweenSwoopsDuration, resyncAngleFromPosition: false);
            else if (hasStolenLetter)
                break;
        }

        if (hasStolenLetter)
        {
            if (letterFlightRoutine != null)
                yield return letterFlightRoutine;

            carriedLetterVisual.SetActive(true);
            yield return CarryAndDropLetter();
        }

        yield return EscapeAndDespawn();
    }

    /// <summary>
    /// Place the flyer outside the player's current vision (light radius / camera frustum).
    /// </summary>
    private void RelocateOutsidePlayerVision()
    {
        if (player == null) return;

        float visionRadius = GetPlayerVisionRadius() + spawnVisionPadding;
        Vector2 dir = Random.insideUnitCircle;
        if (dir.sqrMagnitude < 0.01f)
            dir = Vector2.right;
        dir.Normalize();

        Vector3 spawnPos = player.position + (Vector3)(dir * visionRadius);
        SetFlyerPosition(spawnPos);
        circleAngle = Mathf.Atan2(dir.y, dir.x);
        SetAnimatorDirection(player.position - spawnPos);
    }

    /// <summary>
    /// Vision ≈ max(player Light2D / CameraFollow light radii, camera half-diagonal).
    /// Scene values: mazeLightRadius 6, courtyardLightRadius 9.
    /// </summary>
    private float GetPlayerVisionRadius()
    {
        float radius = 8f;

        CameraFollow camFollow = FindFirstObjectByType<CameraFollow>();
        if (camFollow != null)
        {
            if (camFollow.mainLight2D != null)
                radius = Mathf.Max(radius, camFollow.mainLight2D.pointLightOuterRadius);
            else
                radius = Mathf.Max(camFollow.mazeLightRadius, camFollow.courtyardLightRadius);
        }

        // Player candle / child lights (often the real fog-of-war radius in maze)
        if (player != null)
        {
            var playerLights = player.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true);
            foreach (var light in playerLights)
            {
                if (light != null && light.pointLightOuterRadius > 0.1f)
                    radius = Mathf.Max(radius, light.pointLightOuterRadius);
            }
        }

        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float camCorner = Mathf.Sqrt(halfW * halfW + halfH * halfH);
            radius = Mathf.Max(radius, camCorner);
        }

        return radius;
    }

    private IEnumerator ApproachCirclePerimeter()
    {
        // Pick a perimeter entry point in the direction we spawned from
        Vector3 offset = transform.position - player.position;
        if (offset.sqrMagnitude < 0.01f)
            offset = Vector3.right;
        circleAngle = Mathf.Atan2(offset.y, offset.x);

        while (true)
        {
            Vector3 target = GetCirclePoint(circleAngle);
            Vector3 toTarget = target - transform.position;
            float dist = toTarget.magnitude;

            if (dist <= 0.15f)
            {
                SetFlyerPosition(target);
                break;
            }

            float step = approachSpeed * Time.deltaTime;
            Vector3 next = Vector3.MoveTowards(transform.position, target, step);
            MoveFlyerAndFace(next);
            yield return null;
        }
    }

    /// <param name="resyncAngleFromPosition">
    /// When false, keep the existing <see cref="circleAngle"/> (used after a swoop so we
    /// continue from the exact opposite-perimeter end with no Atan2 wrap / radius snap).
    /// </param>
    private IEnumerator CirclePlayerForDuration(float duration, bool resyncAngleFromPosition = true)
    {
        float timer = 0f;

        if (resyncAngleFromPosition)
        {
            Vector3 offset = transform.position - player.position;
            if (offset.sqrMagnitude > 0.01f)
                circleAngle = Mathf.Atan2(offset.y, offset.x);
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            circleAngle += circleAngularSpeed * Time.deltaTime;

            Vector3 newPos = GetCirclePoint(circleAngle);
            MoveFlyerAndFace(newPos);
            yield return null;
        }
    }

    private Vector3 GetCirclePoint(float angle)
    {
        Vector3 center = player.position;
        return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * circleDistance;
    }

    /// <summary>
    /// Animator Any-State transitions require |secondary axis| &lt; 0.2, so diagonal
    /// tangents never fire Up/Down/Left/Right. Snap to the dominant cardinal.
    /// </summary>
    private void SetAnimatorDirection(Vector3 move)
    {
        if (animator == null) return;
        if (move.sqrMagnitude < 0.0001f) return;

        Vector2 dir = ((Vector2)move).normalized;
        lastFacingDir = dir;

        float moveX;
        float moveY;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            moveX = Mathf.Sign(dir.x);
            moveY = 0f;
        }
        else
        {
            moveX = 0f;
            moveY = Mathf.Sign(dir.y);
        }

        animator.SetFloat("MoveX", moveX);
        animator.SetFloat("MoveY", moveY);
    }

    private void MoveFlyerAndFace(Vector3 newPos)
    {
        Vector3 from = GetFlyerPosition();
        Vector3 delta = newPos - from;
        SetFlyerPosition(newPos);
        if (delta.sqrMagnitude > 0.00001f)
            SetAnimatorDirection(delta);
        else if (lastFacingDir.sqrMagnitude > 0.01f)
            SetAnimatorDirection(lastFacingDir);
    }

    private Vector3 GetFlyerPosition()
    {
        return transform.position;
    }

    private void SetFlyerPosition(Vector3 worldPos)
    {
        // Write transform immediately so facing deltas are not one frame behind RB sync
        transform.position = worldPos;
        if (rb != null)
            rb.position = worldPos;
    }

    private IEnumerator SwoopAttempt()
    {
        Debug.Log($"[FlyerDebug] Swoop attempt {swoopAttempts + 1} started. Player has {playerInventory.collectedLetters.Count} letters.");
        flyerCollider.enabled = false;

        PlaySfx(swoopWhooshClip);

        // Lock swoop to current circle angle → opposite perimeter (180°).
        // Do not snap onto a different start point — continue from the live orbit pose.
        Vector3 liveOffset = transform.position - player.position;
        if (liveOffset.sqrMagnitude > 0.01f)
            circleAngle = Mathf.Atan2(liveOffset.y, liveOffset.x);

        float startAngle = circleAngle;
        float endAngle = startAngle + Mathf.PI;

        float t = 0f;
        bool hasTriedSteal = false;

        while (t < 1f)
        {
            t += Time.deltaTime / swoopTime;
            // Fast for most of the swoop; slight taper only near the end (no mid-pause)
            float sampleT = FastSwoopThenTaper(Mathf.Clamp01(t));

            Vector3 liveStart = GetCirclePoint(startAngle);
            Vector3 liveEnd = GetCirclePoint(endAngle);
            Vector3 liveControl = player.position + Vector3.down * swoopDipDepth;

            Vector3 newPos = EvaluateQuadraticBezier(liveStart, liveControl, liveEnd, sampleT);
            Vector3 tangent = EvaluateQuadraticBezierTangent(liveStart, liveControl, liveEnd, sampleT);
            SetFlyerPosition(newPos);
            if (tangent.sqrMagnitude > 0.0001f)
                SetAnimatorDirection(tangent);
            else if (lastFacingDir.sqrMagnitude > 0.01f)
                SetAnimatorDirection(lastFacingDir);

            // Steal at closest approach without freezing movement
            if (!hasTriedSteal && sampleT >= 0.5f)
            {
                hasTriedSteal = true;
                TryStealLetter();
            }

            yield return null;
        }

        // Exact opposite-perimeter end; preserve this angle for continuous orbit resume
        circleAngle = endAngle;
        SetFlyerPosition(GetCirclePoint(endAngle));

        flyerCollider.enabled = true;
    }

    /// <summary>
    /// Nearly linear (fast) progress for most of the swoop; ease-out only after <paramref name="taperStart"/>.
    /// </summary>
    private static float FastSwoopThenTaper(float t, float taperStart = 0.78f)
    {
        t = Mathf.Clamp01(t);
        if (taperStart >= 1f)
            return t;
        if (t <= taperStart)
            return t;

        float u = (t - taperStart) / (1f - taperStart);
        // Quadratic ease-out on the final segment only
        float eased = 1f - (1f - u) * (1f - u);
        return taperStart + (1f - taperStart) * eased;
    }

    private static Vector3 EvaluateQuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private static Vector3 EvaluateQuadraticBezierTangent(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        // B'(t) = 2(1-t)(b-a) + 2t(c-b)
        return 2f * (1f - t) * (b - a) + 2f * t * (c - b);
    }

    private static float EaseOutCubic(float x)
    {
        float inv = 1f - x;
        return 1f - inv * inv * inv;
    }

    private void TryStealLetter()
    {
        if (playerInventory.hasItem && Random.value < stealSuccessPercent)
        {
            var letters = playerInventory.collectedLetters;
            if (letters.Count > 0)
            {
                carriedLetterObject = letters[0];

                bool removed = playerInventory.RemoveLetter(carriedLetterObject);
                Debug.Log($"[FlyerDebug] RemoveLetter returned {removed}. Player now has {playerInventory.collectedLetters.Count} letters.");

                hasStolenLetter = true;
                glowController?.SetGlowColor(new Color(2.5f, 0.6f, 0f, 1f));
                PlaySfx(stealClip);
                Debug.Log($"[FlyerDebug] Flyer stole letter '{carriedLetterObject.letter}' from player.");

                if (letterFlightRoutine != null)
                    StopCoroutine(letterFlightRoutine);
                letterFlightRoutine = StartCoroutine(LetterFlightToCarrySocket(carriedLetterObject));
            }
        }
        else
        {
            Debug.Log("[FlyerDebug] Swoop attempt failed: no letter stolen.");
        }
    }

    private IEnumerator LetterFlightToCarrySocket(LetterObject letter)
    {
        if (letter == null || carriedLetterVisual == null)
            yield break;

        letter.transform.SetParent(null, true);

        if (!letter.gameObject.activeSelf)
            letter.gameObject.SetActive(true);

        if (letter.TryGetComponent<SpriteRenderer>(out var sr))
            sr.enabled = true;
        if (letter.TryGetComponent<Collider2D>(out var col))
            col.enabled = false;

        if (!carriedLetterVisual.activeSelf)
            carriedLetterVisual.SetActive(true);

        Vector3 flightStart = player != null ? player.position : letter.transform.position;
        letter.transform.position = flightStart;

        float duration = Mathf.Max(0.15f, letterFlightDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(u);

            Vector3 target = carriedLetterVisual.transform.position;
            Vector3 mid = Vector3.Lerp(flightStart, target, 0.5f) + Vector3.up * 0.45f;
            letter.transform.position = EvaluateQuadraticBezier(flightStart, mid, target, eased);
            yield return null;
        }

        letter.transform.SetParent(carriedLetterVisual.transform, false);
        letter.transform.localPosition = Vector3.zero;
        letterFlightRoutine = null;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;

        GameObject soundObj = new GameObject("FlyerSfx");
        soundObj.transform.position = transform.position;
        AudioSource source = soundObj.AddComponent<AudioSource>();
        source.clip = clip;
        source.spatialBlend = 0f;
        source.volume = sfxVolume;
        source.Play();
        Destroy(soundObj, clip.length + 0.1f);
    }

    private IEnumerator CarryAndDropLetter()
    {
        dropPosition = FindDropPosition();
        float travelTime = 1.5f;
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / travelTime;
            Vector3 newPos = Vector3.Lerp(start, dropPosition, t);
            MoveFlyerAndFace(newPos);
            yield return null;
        }

        if (carriedLetterObject != null)
        {
            carriedLetterObject.transform.SetParent(null);
            carriedLetterObject.transform.position = dropPosition;
            if (carriedLetterObject.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = true;
            if (carriedLetterObject.TryGetComponent<Collider2D>(out var col)) col.enabled = true;
            Debug.Log($"[FlyerDebug] Flyer dropped letter '{carriedLetterObject.letter}' at {dropPosition}.");

            carriedLetterObject = null;
            carriedLetterVisual.SetActive(false);
            glowController?.SetGlowColor(new Color(0.3f, 1.0f, 0.9f, 1f));
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator EscapeAndDespawn()
    {
        Vector3 escapeDir = (transform.position - player.position).normalized;
        if (escapeDir.sqrMagnitude < 0.01f)
            escapeDir = Vector3.right;
        Vector3 escapeTarget = transform.position + escapeDir * 20f;
        float t = 0f;
        float escapeTime = 1.5f;
        Vector3 start = transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime / escapeTime;
            Vector3 newPos = Vector3.Lerp(start, escapeTarget, t);
            MoveFlyerAndFace(newPos);
            yield return null;
        }

        Destroy(gameObject);
    }

    private Vector3 FindDropPosition()
    {
        AStarGridManager grid = FindFirstObjectByType<AStarGridManager>();

        List<Node> walkableNodes = grid.GetAllWalkableNodes();
        List<Node> validNodes = new List<Node>();

        foreach (Node node in walkableNodes)
        {
            float distance = Vector3.Distance(player.position, node.worldPosition);
            if (distance < minDropDistanceFromPlayer || distance > maxDropDistanceFromPlayer)
                continue;

            validNodes.Add(node);
        }

        if (validNodes.Count > 0)
        {
            Node chosen = validNodes[Random.Range(0, validNodes.Count)];
            return chosen.worldPosition;
        }

        Debug.LogWarning("[Flyer] No valid drop tiles found. Using random walkable tile.");

        Node fallback = grid.GetRandomWalkableNode();
        if (fallback != null)
            return fallback.worldPosition;

        return player.position + (Vector3)(Random.insideUnitCircle.normalized * minDropDistanceFromPlayer);
    }
}
