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

    [Header("Swoop")]
    [SerializeField] private float swoopTime = 1.15f;
    [SerializeField] private float swoopDipDepth = 1.1f;
    [SerializeField] private float nadirHoldDuration = 0.12f;
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


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        carriedLetterVisual.SetActive(false);
        flyerCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        // Prefer kinematic so Bezier / position writes are not fought by Dynamic physics
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
        StartCoroutine(FlyerRoutine());
    }

    private IEnumerator FlyerRoutine()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();

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
            if (!hasStolenLetter && swoopAttempts < maxSwoopAttempts)
            {
                yield return CirclePlayerForDuration(circleBetweenSwoopsDuration);
            }
        }

        if (hasStolenLetter)
        {
            // Wait for letter flight tween to finish parenting before carry/drop
            if (letterFlightRoutine != null)
                yield return letterFlightRoutine;

            carriedLetterVisual.SetActive(true);
            yield return CarryAndDropLetter();
        }

        yield return EscapeAndDespawn();
    }


    private IEnumerator CirclePlayerForDuration(float duration)
    {
        float timer = 0f;
        float radius = circleDistance;
        Vector3 offset = transform.position - player.position;
        float angle = Mathf.Atan2(offset.y, offset.x);

        float speed = 2f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            angle += speed * Time.deltaTime;

            Vector3 center = player.position;
            Vector3 newOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Vector3 newPos = center + newOffset;
            SetAnimatorDirection(newPos - transform.position);
            SetFlyerPosition(newPos);
            yield return null;
        }
    }

    private void SetAnimatorDirection(Vector3 move)
    {
        if (animator == null) return;
        if (move.magnitude < 0.01f) return;

        Vector3 normalized = move.normalized;
        animator.SetFloat("MoveX", normalized.x);
        animator.SetFloat("MoveY", normalized.y);
    }

    private void SetFlyerPosition(Vector3 worldPos)
    {
        if (rb != null)
            rb.position = worldPos;
        else
            transform.position = worldPos;
    }

    private IEnumerator SwoopAttempt()
    {
        Debug.Log($"[FlyerDebug] Swoop attempt {swoopAttempts + 1} started. Player has {playerInventory.collectedLetters.Count} letters.");
        flyerCollider.enabled = false;

        PlaySfx(swoopWhooshClip);

        // Start from current position (no first-frame snap onto a distant curve start)
        Vector3 start = transform.position;
        Vector3 center = player.position;
        Vector3 flyerToCenter = (start - center);
        if (flyerToCenter.sqrMagnitude < 0.0001f)
            flyerToCenter = Vector3.right;
        flyerToCenter.Normalize();

        Vector3 end = center - flyerToCenter * circleDistance;
        Vector3 control = player.position + Vector3.down * swoopDipDepth;

        float t = 0f;
        bool hasTriedSteal = false;
        bool heldAtNadir = false;

        while (t < 1f)
        {
            float dt = Time.deltaTime / swoopTime;
            t += dt;

            // Ease climb-out after nadir for a clearer pull-away
            float sampleT = t <= 0.5f
                ? t
                : 0.5f + 0.5f * EaseOutCubic((t - 0.5f) / 0.5f);

            sampleT = Mathf.Clamp01(sampleT);
            Vector3 newPos = EvaluateQuadraticBezier(start, control, end, sampleT);
            SetAnimatorDirection(newPos - transform.position);
            SetFlyerPosition(newPos);

            if (!hasTriedSteal && t >= 0.5f)
            {
                hasTriedSteal = true;

                // Brief nadir hold so the steal / miss reads as contact
                if (!heldAtNadir && nadirHoldDuration > 0f)
                {
                    heldAtNadir = true;
                    TryStealLetter();
                    yield return new WaitForSeconds(nadirHoldDuration);
                    continue;
                }

                TryStealLetter();
            }

            yield return null;
        }

        SetFlyerPosition(end);

        yield return new WaitForSeconds(0.35f);
        flyerCollider.enabled = true;
    }

    private static Vector3 EvaluateQuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
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

        // Visible at player world position, unparented, then curve to carry socket
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
            // Slight upward arc so the snatch reads in screen space
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
            SetAnimatorDirection(newPos - transform.position);
            SetFlyerPosition(newPos);
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
        Vector3 escapeTarget = transform.position + escapeDir * 20f;
        float t = 0f;
        float escapeTime = 1.5f;
        Vector3 start = transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime / escapeTime;
            Vector3 newPos = Vector3.Lerp(start, escapeTarget, t);
            SetAnimatorDirection(newPos - transform.position);
            SetFlyerPosition(newPos);
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
