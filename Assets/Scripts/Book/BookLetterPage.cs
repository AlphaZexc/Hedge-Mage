using System.Collections.Generic;
using UnityEngine;

public class BookLetterPage : MonoBehaviour
{
    public static BookLetterPage Instance;

    [Header("Spawn Settings")]
    public DraggableLetterUI letterPrefab;
    public Transform letterContainer;

    private List<DraggableLetterUI> spawnedLetters = new List<DraggableLetterUI>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        RefreshFromInventory();
    }

    public void RefreshFromInventory()
    {
        ClearPage();

        foreach (var letterObj in PlayerInventory.Instance.collectedLetters)
        {
            SpawnLetter(letterObj.letter);
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