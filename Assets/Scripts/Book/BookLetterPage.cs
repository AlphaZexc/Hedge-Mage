using System.Collections.Generic;
using UnityEngine;

public class BookLetterPage : MonoBehaviour
{
    [Header("Spawn Settings")]
    public DraggableLetterUI letterPrefab;
    public Transform letterContainer;

    [Header("Letters On This Page")]
    public List<char> lettersToSpawn = new List<char>();

    private List<DraggableLetterUI> spawnedLetters = new List<DraggableLetterUI>();

    private void Start()
    {
        SpawnLetters(lettersToSpawn);
    }

    public void SpawnLetters(List<char> letters)
    {
        ClearPage();

        foreach (char c in letters)
        {
            SpawnLetter(c);
        }
    }

    private void SpawnLetter(char c)
    {
        char upper = char.ToUpper(c);

        DraggableLetterUI newLetter =
            Instantiate(letterPrefab, letterContainer);

        newLetter.Initialize(upper);

        spawnedLetters.Add(newLetter);
    }

    public void ClearPage()
    {
        foreach (var letter in spawnedLetters)
        {
            if (letter != null)
                Destroy(letter.gameObject);
        }

        spawnedLetters.Clear();
    }
}