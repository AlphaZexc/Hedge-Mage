using System.Collections.Generic;
using UnityEngine;

public class LetterManager : MonoBehaviour
{
    public static LetterManager Instance;

    public GameObject letterPrefab;
    public List<Transform> spawnPoints;
    public int numberOfDecoys = 5;

    private List<GameObject> spawnedLetters = new List<GameObject>();
    private List<Transform> availableSpots = new List<Transform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    private void Start()
    {
        SpawnLetters(WordProgressManager.Instance.targetWord);
    }

    public void ResetLettersForNewWord(string newWord)
    {
        foreach (var obj in spawnedLetters)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedLetters.Clear();

        SpawnLetters(newWord);
    }

    private void SpawnLetters(string targetWord)
    {
        availableSpots = new List<Transform>(spawnPoints);

        // Shuffle spots once upfront for randomness
        for (int i = availableSpots.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (availableSpots[i], availableSpots[j]) = (availableSpots[j], availableSpots[i]);
        }

        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string upperWord = targetWord.ToUpper();

        // Count letters in target word
        Dictionary<char, int> letterCounts = new Dictionary<char, int>();
        foreach (char c in upperWord)
        {
            if (letterCounts.ContainsKey(c)) letterCounts[c]++;
            else letterCounts[c] = 1;
        }

        // Spawn correct letters
        foreach (var kvp in letterCounts)
            for (int i = 0; i < kvp.Value; i++)
                SpawnLetter(kvp.Key);

        // Spawn guaranteed letters (skip if already spawned for the target word)
        List<char> guaranteed = new List<char> { 'J', 'U', 'M', 'P', 'F', 'I', 'R', 'E', 'B', 'A', 'L', 'S', 'T', 'O' };
        foreach (char c in guaranteed)
            SpawnLetter(c); // Duplicates are fine — extra copies are valid

        // Spawn decoys — build a safe candidate list first
        List<char> decoyPool = new List<char>();
        foreach (char c in alphabet)
            if (!upperWord.Contains(c.ToString()))
                decoyPool.Add(c);

        // Shuffle decoy pool
        for (int i = decoyPool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (decoyPool[i], decoyPool[j]) = (decoyPool[j], decoyPool[i]);
        }

        int decoysPlaced = 0;
        foreach (char c in decoyPool)
        {
            if (decoysPlaced >= numberOfDecoys || availableSpots.Count == 0) break;
            SpawnLetter(c);
            decoysPlaced++;
        }

        Debug.Log($"Spawned {spawnedLetters.Count} letters for word: {targetWord}");
    }

    private void SpawnLetter(char letter)
    {
        if (availableSpots.Count == 0)
        {
            Debug.LogWarning($"No available spots left — could not spawn: {letter}");
            return;
        }

        // Since spots are pre-shuffled, just take the last one (O(1) removal)
        int index = availableSpots.Count - 1;
        Transform spot = availableSpots[index];
        availableSpots.RemoveAt(index);

        GameObject obj = Instantiate(letterPrefab, spot.position, Quaternion.identity);
        obj.SetActive(true);
        spawnedLetters.Add(obj);

        LetterObject letterObj = obj.GetComponent<LetterObject>();
        if (letterObj != null)
            letterObj.SetLetter(letter);
    }
}

