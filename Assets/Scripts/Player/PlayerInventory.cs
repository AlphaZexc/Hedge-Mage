using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    // List of collected LetterObjects
    public List<LetterObject> collectedLetters = new List<LetterObject>();

    private void Start()
    {
        // Notify CreatureManager that the player is ready
        if (CreatureManager.Instance != null)
        {
            CreatureManager.Instance.NotifyPlayerReady(this);
        }
    }

    public bool HasItem() => collectedLetters.Count > 0;
    public int LetterCount => collectedLetters.Count;

    public GameObject TakeItem()
    {
        // Remove and return the first collected letter (FIFO)
        if (collectedLetters.Count == 0) return null;
        LetterObject letterObj = collectedLetters[0];
        collectedLetters.RemoveAt(0);
        // Optionally: destroy the object here if needed, or let the caller handle it
        return letterObj != null ? letterObj.gameObject : null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        LetterObject letterObj = other.GetComponent<LetterObject>();
        if (letterObj != null)
        {
            char collectedChar = letterObj.letter;
            // Report to WordProgressManager
            WordProgressManager progress = FindFirstObjectByType<WordProgressManager>();
            if (progress != null)
            {
                progress.CollectLetter(collectedChar);
            }
            // Add to inventory, do not destroy yet
            collectedLetters.Add(letterObj);
            // Optionally: disable the letter's collider/renderer to hide it from the world
            if (letterObj.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
            if (letterObj.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = false;
            Debug.Log($"Collected letter: {collectedChar}");
        }
    }

    // Utility: get all collected letters
    public List<LetterObject> GetCollectedLetters() => new List<LetterObject>(collectedLetters);

    // Utility: remove a specific letter (by reference)
    public bool RemoveLetter(LetterObject letter)
    {
        return collectedLetters.Remove(letter);
    }
}
