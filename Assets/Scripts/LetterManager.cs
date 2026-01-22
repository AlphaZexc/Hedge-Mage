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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SpawnLetters(WordProgressManager.Instance.GetTargetWord());
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
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // 🔥 NEW: Count duplicates correctly
        Dictionary<char, int> letterCounts = new Dictionary<char, int>();
        foreach (char c in targetWord)
        {
            char upper = char.ToUpper(c);
            if (letterCounts.ContainsKey(upper))
                letterCounts[upper]++;
            else
                letterCounts[upper] = 1;
        }

        // Spawn correct letters (including duplicates)
        foreach (var kvp in letterCounts)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                SpawnLetter(kvp.Key);
            }
        }

        // Spawn decoys
        int decoysPlaced = 0;
        while (decoysPlaced < numberOfDecoys && availableSpots.Count > 0)
        {
            char randomLetter = alphabet[Random.Range(0, alphabet.Length)];
            if (!targetWord.Contains(randomLetter.ToString()))
            {
                SpawnLetter(randomLetter);
                decoysPlaced++;
            }
        }
        
        Debug.Log($"Spawned {spawnedLetters.Count} letters for word: {targetWord}");

    }

    // INSERTED 5/2
    private void SpawnLetter(char letter)
    {
        if (availableSpots.Count == 0) return;

        int index = Random.Range(0, availableSpots.Count);
        Transform spot = availableSpots[index];
        availableSpots.RemoveAt(index);

        // Check for existing letter at the spot
        Collider2D hit = Physics2D.OverlapCircle(spot.position, 0.1f, LayerMask.GetMask("Letter"));
        if (hit != null)
        {
            Debug.LogWarning($"Spawn blocked at {spot.position}, already occupied by: {hit.gameObject.name}");
            return; // Skip this spawn to avoid overlap
        }

        GameObject obj = Instantiate(letterPrefab, spot.position, Quaternion.identity);
        obj.SetActive(true);
        spawnedLetters.Add(obj);

        LetterObject letterObj = obj.GetComponent<LetterObject>();
        if (letterObj != null)
        {
            letterObj.SetLetter(letter);
        }
    }


    // private void SpawnLetter(char letter)
    // {
    //     if (availableSpots.Count == 0) return;

    //     int index = Random.Range(0, availableSpots.Count);
    //     Transform spot = availableSpots[index];
    //     availableSpots.RemoveAt(index);

    //     GameObject obj = Instantiate(letterPrefab, spot.position, Quaternion.identity);
    //     obj.SetActive(true);
    //     spawnedLetters.Add(obj);

    //     LetterObject letterObj = obj.GetComponent<LetterObject>();
    //     if (letterObj != null)
    //     {
    //         letterObj.SetLetter(letter);
    //     }
    // }
}


