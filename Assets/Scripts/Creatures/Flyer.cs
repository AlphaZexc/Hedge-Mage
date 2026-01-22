using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class Flyer : MonoBehaviour
{
    public float cooldown = 60f;
    public int maxSwoopAttempts = 3;
    [Range(0f, 1f)] public float stealSuccessPercent = 1.0f; // Set to 1.0 for testing
    public float minDropDistanceFromPlayer = 5f;
    public float flyOverDuration = 2f;
    public GameObject carriedLetterVisual; // Assign a child GameObject to show the letter

    private Transform player;
    private PlayerInventory playerInventory;
    private LetterObject carriedLetterObject;
    private int swoopAttempts = 0;
    private Vector3 dropPosition;
    private bool hasStolenLetter = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerInventory = player.GetComponent<PlayerInventory>();
        carriedLetterVisual.SetActive(false);
        StartCoroutine(FlyerRoutine());
    }

    IEnumerator FlyerRoutine()
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

    void CirclePlayer()
    {
        // Simple circling logic (can be replaced with animation/path)
        float radius = 4f;
        float speed = 2f;
        float angle = Time.time * speed;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
        transform.position = player.position + offset;
    }

    IEnumerator SwoopAttempt()
    {
        int invCount = playerInventory != null ? playerInventory.GetCollectedLetters().Count : -1;
        Debug.Log($"[FlyerDebug] Swoop attempt {swoopAttempts + 1} started. Player has {invCount} letters.");
        // Swoop over player, try to steal letter
        float swoopTime = 1f;
        Vector3 start = transform.position;
        Vector3 end = player.position + Vector3.up * 2f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / swoopTime;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        // Attempt to steal
        if (playerInventory != null && playerInventory.HasItem() && Random.value < stealSuccessPercent)
        {
            // Take the first letter from the player's inventory
            var letters = playerInventory.GetCollectedLetters();
            if (letters.Count > 0)
            {
                carriedLetterObject = letters[0];
                bool removed = playerInventory.RemoveLetter(carriedLetterObject);
                Debug.Log($"[FlyerDebug] RemoveLetter returned {removed}. Player now has {playerInventory.GetCollectedLetters().Count} letters.");
                // Attach to Flyer visual
                carriedLetterObject.transform.SetParent(carriedLetterVisual.transform);
                carriedLetterObject.transform.localPosition = Vector3.zero;
                carriedLetterObject.gameObject.SetActive(true);
                if (carriedLetterObject.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = true;
                if (carriedLetterObject.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
                carriedLetterVisual.SetActive(true);
                hasStolenLetter = true;
                Debug.Log($"[FlyerDebug] Flyer stole letter '{carriedLetterObject.letter}' from player.");
            }
        }
        else
        {
            Debug.Log("[FlyerDebug] Swoop attempt failed: no letter stolen.");
        }
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator CarryAndDropLetter()
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
            transform.position = Vector3.Lerp(start, dropPosition, t);
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
        }
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator EscapeAndDespawn()
    {
        // Move off screen and destroy
        Vector3 escapeDir = (transform.position - player.position).normalized;
        Vector3 escapeTarget = transform.position + escapeDir * 10f;
        float t = 0f;
        float escapeTime = 1.5f;
        Vector3 start = transform.position;
        while (t < 1f)
        {
            t += Time.deltaTime / escapeTime;
            transform.position = Vector3.Lerp(start, escapeTarget, t);
            yield return null;
        }
        Destroy(gameObject);
    }

    Vector3 FindDropPosition()
    {
        // TODO: Implement logic to find a Pathway tile at minDropDistanceFromPlayer
        // For now, just pick a random point away from player
        Vector3 randomDir = Random.insideUnitCircle.normalized;
        Vector3 candidate = player.position + (Vector3)randomDir * minDropDistanceFromPlayer;
        candidate.z = 0;
        return candidate;
    }
}
