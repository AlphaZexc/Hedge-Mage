using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class Flyer : MonoBehaviour
{
    [Header("General")]
    public float cooldown = 60f;
    public GameObject carriedLetterVisual; // Assign a child GameObject to show the letter

    [Header("Movement")]
    public float flyOverDuration = 2f;
    public float circleDistance = 4f;
    public float circleBetweenSwoopsDuration = 1.5f; // New: circling time between swoops
    public float minDropDistanceFromPlayer = 5f;
    public int maxSwoopAttempts = 3;
    [Range(0f, 1f)] public float stealSuccessPercent = 0.3f; // Set to 1.0 for testing

    private Animator animator; // Reference to the Animator
    private FlyerGlowController glowController; // Reference to the glow controller

    private Collider2D flyerCollider;
    private Transform player;
    private PlayerInventory playerInventory => PlayerInventory.Instance;
    private LetterObject carriedLetterObject;
    private int swoopAttempts = 0;
    private Vector3 dropPosition;
    private bool hasStolenLetter = false;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        carriedLetterVisual.SetActive(false);
        flyerCollider = GetComponent<Collider2D>();

        // Get Animator from child 'Visuals'
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
        // Initial circling phase
        yield return CirclePlayerForDuration(flyOverDuration);

        // Swoop attempts with circling in between
        while (swoopAttempts < maxSwoopAttempts && !hasStolenLetter)
        {
            yield return SwoopAttempt();
            swoopAttempts++;
            if (!hasStolenLetter && swoopAttempts < maxSwoopAttempts)
            {
                yield return CirclePlayerForDuration(circleBetweenSwoopsDuration);
            }
        }

        // If letter stolen, carry and drop
        if (hasStolenLetter)
        {
            carriedLetterVisual.SetActive(true);
            yield return CarryAndDropLetter();
        }
        // Escape
        yield return EscapeAndDespawn();
    }


    private IEnumerator CirclePlayerForDuration(float duration)
    {
        float timer = 0f;
        // Calculate the current angle based on the Flyer's position
        float radius = circleDistance;
        Vector3 offset = transform.position - player.position;
        float angle = Mathf.Atan2(offset.y, offset.x);

        float speed = 2f; // radians per second

        while (timer < duration)
        {
            timer += Time.deltaTime;
            angle += speed * Time.deltaTime;

            // Always use the latest player position as the center
            Vector3 center = player.position;
            Vector3 newOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Vector3 newPos = center + newOffset;
            SetAnimatorDirection(newPos - transform.position);
            transform.position = newPos;
            yield return null;
        }
    }



    // Helper to set animator direction based on movement vector
    private void SetAnimatorDirection(Vector3 move)
    {
        if (animator == null) return;
        if (move.magnitude < 0.01f) return;
        
        // Normalize the movement vector
        Vector3 normalized = move.normalized;
        
        // Set animator parameters for smooth transitions
        animator.SetFloat("MoveX", normalized.x);
        animator.SetFloat("MoveY", normalized.y);
    }

    private IEnumerator SwoopAttempt()
    {
        Debug.Log($"[FlyerDebug] Swoop attempt {swoopAttempts + 1} started. Player has {playerInventory.collectedLetters.Count} letters.");
        flyerCollider.enabled = false;

        // Calculate circle parameters
        float radius = circleDistance;
        Vector3 center = player.position;
        Vector3 flyerToCenter = (transform.position - center).normalized;
        Vector3 start = center + flyerToCenter * radius;

        // Find the opposite point on the circle
        Vector3 end = center - flyerToCenter * radius;

        // Control point: at player's position, but lower (for a nice arc)
        Vector3 control = player.position + Vector3.down * 1.1f;

        float swoopTime = 1f;
        float t = 0f;
        bool hasTriedSteal = false;

        while (t < 1f)
        {
            t += Time.deltaTime / swoopTime;
            Vector3 newPos = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * control + Mathf.Pow(t, 2) * end;
            SetAnimatorDirection(newPos - transform.position);
            transform.position = newPos;

            if (!hasTriedSteal && t >= 0.5f)
            {
                hasTriedSteal = true;
                TryStealLetter();
            }

            yield return null;
        }

        // Snap to end point to avoid large jumps in next phase
        transform.position = end;

        yield return new WaitForSeconds(0.5f);
        flyerCollider.enabled = true;

    }

    private void TryStealLetter()
    {
        if (playerInventory.hasItem && Random.value < stealSuccessPercent)
        {
            var letters = playerInventory.collectedLetters;
            if (letters.Count > 0)
            {
                carriedLetterObject = letters[0];

                // Only update if not already parented/active
                if (carriedLetterObject.transform.parent != carriedLetterVisual.transform)
                    carriedLetterObject.transform.SetParent(carriedLetterVisual.transform, false);

                if (!carriedLetterVisual.activeSelf)
                    carriedLetterVisual.SetActive(true);

                if (!carriedLetterObject.gameObject.activeSelf)
                    carriedLetterObject.gameObject.SetActive(true);

                if (carriedLetterObject.TryGetComponent<SpriteRenderer>(out var sr) && !sr.enabled)
                    sr.enabled = true;
                if (carriedLetterObject.TryGetComponent<Collider2D>(out var col) && col.enabled)
                    col.enabled = false;

                carriedLetterObject.transform.localPosition = Vector3.zero;

                hasStolenLetter = true;
                glowController?.SetGlowColor(new Color(2.5f, 0.6f, 0f, 1f));
                Debug.Log($"[FlyerDebug] Flyer stole letter '{carriedLetterObject.letter}' from player.");

                bool removed = playerInventory.RemoveLetter(carriedLetterObject);
                Debug.Log($"[FlyerDebug] RemoveLetter returned {removed}. Player now has {playerInventory.collectedLetters.Count} letters.");
            }
        }
        else
        {
            Debug.Log("[FlyerDebug] Swoop attempt failed: no letter stolen.");
        }
    }

    private IEnumerator CarryAndDropLetter()
    {
        // Find a valid drop position on a Pathway tile, away from player
        dropPosition = FindDropPosition();
        // Fly to drop position
        float travelTime = 1.5f;
        Vector3 start = transform.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / travelTime;
            Vector3 newPos = Vector3.Lerp(start, dropPosition, t);
            SetAnimatorDirection(newPos - transform.position);
            transform.position = newPos;
            yield return null;
        }
        // Drop the letter
        if (carriedLetterObject != null)
        {
            carriedLetterObject.transform.SetParent(null);
            carriedLetterObject.transform.position = dropPosition;
            if (carriedLetterObject.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = true;
            if (carriedLetterObject.TryGetComponent<Collider2D>(out var col)) col.enabled = true;
            Debug.Log($"[FlyerDebug] Flyer dropped letter '{carriedLetterObject.letter}' at {dropPosition}.");

            carriedLetterObject = null;
            carriedLetterVisual.SetActive(false);
            // Restore default glow colour after dropping the letter
            glowController?.SetGlowColor(new Color(0.3f, 1.0f, 0.9f, 1f));
        }
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator EscapeAndDespawn()
    {
        // Move off screen and destroy
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
            transform.position = newPos;
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
            // Check minimum distance from player
            float distance = Vector3.Distance(player.position, node.worldPosition);
            if (distance < minDropDistanceFromPlayer)
                continue;

            validNodes.Add(node);
        }

        if (validNodes.Count > 0)
        {
            Node chosen = validNodes[Random.Range(0, validNodes.Count)];
            return chosen.worldPosition;
        }

        Debug.LogWarning("[Flyer] No valid drop tiles found. Using random walkable tile.");

        // Fallback: pick any random walkable node
        Node fallback = grid.GetRandomWalkableNode();
        if (fallback != null)
            return fallback.worldPosition;

        // Absolute fallback
        return player.position + (Vector3)(Random.insideUnitCircle.normalized * minDropDistanceFromPlayer);
    }
}
