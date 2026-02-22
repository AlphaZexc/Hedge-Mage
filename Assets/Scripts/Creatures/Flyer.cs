using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class Flyer : MonoBehaviour
{
    public float cooldown = 60f;
    public int maxSwoopAttempts = 3;
    [Range(0f, 1f)] public float stealSuccessPercent = 0.3f; // Set to 1.0 for testing
    public float minDropDistanceFromPlayer = 5f;
    public float flyOverDuration = 2f;
    public GameObject carriedLetterVisual; // Assign a child GameObject to show the letter

    private Animator animator; // Reference to the Animator
    private FlyerGlowController glowController; // Reference to the glow controller

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
        // 1. Circling phase
        float timer = 0f;
        while (timer < flyOverDuration)
        {
            timer += Time.deltaTime;
            CirclePlayer();
            yield return null;
        }
        // 2. Swoop attempts
        while (swoopAttempts < maxSwoopAttempts && !hasStolenLetter)
        {
            yield return SwoopAttempt();
            swoopAttempts++;
        }
        // 3. If letter stolen, carry and drop
        if (hasStolenLetter)
        {
            carriedLetterVisual.SetActive(true);
            yield return CarryAndDropLetter();
        }
        // 4. Escape
        yield return EscapeAndDespawn();
    }

    private void CirclePlayer()
    {
        // Simple circling logic (can be replaced with animation/path)
        float radius = 4f;
        float speed = 2f;
        float angle = Time.time * speed;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        Vector3 newPos = player.position + offset;
        SetAnimatorDirection(newPos - transform.position);
        transform.position = newPos;
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

        // Swoop over player, try to steal letter
        float swoopTime = 1f;
        Vector3 start = transform.position;
        Vector3 end = player.position + Vector3.up * 2f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / swoopTime;
            Vector3 newPos = Vector3.Lerp(start, end, t);
            SetAnimatorDirection(newPos - transform.position);
            transform.position = newPos;
            yield return null;
        }
        // Attempt to steal
        if (playerInventory.hasItem && Random.value < stealSuccessPercent)
        {
            // Take the first letter from the player's inventory
            var letters = playerInventory.collectedLetters;
            if (letters.Count > 0)
            {
                carriedLetterObject = letters[0];

                Debug.Log("Current carriedLetterObject:" + carriedLetterObject.letter);

                carriedLetterVisual.SetActive(true);
                carriedLetterObject.gameObject.SetActive(true);

                if (carriedLetterObject.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = true;
                if (carriedLetterObject.TryGetComponent<Collider2D>(out var col)) col.enabled = false;

                // Attach to Flyer visual
                carriedLetterObject.transform.SetParent(carriedLetterVisual.transform);
                carriedLetterObject.transform.localPosition = Vector3.zero;


                hasStolenLetter = true;
                // Boost glow colour to red/orange when carrying a stolen letter
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
        yield return new WaitForSeconds(0.5f);
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
        if (grid == null)
        {
            Debug.LogWarning("[Flyer] No AStarGridManager found. Falling back to random position.");
            return player.position + (Vector3)(Random.insideUnitCircle.normalized * minDropDistanceFromPlayer);
        }

        List<Node> walkableNodes = grid.GetAllWalkableNodes();
        List<Node> validNodes = new List<Node>();

        foreach (Node node in walkableNodes)
        {
            // Check minimum distance from player
            float distance = Vector3.Distance(player.position, node.worldPosition);
            if (distance < minDropDistanceFromPlayer)
                continue;

            // Check that player can actually reach this node
            var path = grid.FindPath(player.position, node.worldPosition);
            if (path != null && path.Count > 0)
            {
                validNodes.Add(node);
            }
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
