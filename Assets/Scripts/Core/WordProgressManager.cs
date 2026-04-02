using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordProgressManager : MonoBehaviour
{
    public static WordProgressManager Instance;

    [Header("Popups")]
    public GameObject levelCompletePopup;
    public GameObject levelFailPopup;

    [Header("Letter/Word Information")]
    public string targetWord { get; private set; }
    public bool AllLettersCollected { get; private set; } // Word is complete
    public List<LetterObject> collectedLetters => PlayerInventory.Instance.collectedLetters;

    [SerializeField] private string[] wordList = { "APPLE", "HOUSE", "LIGHT", "BRICK", "WATER" };

    private int currentWordIndex = 0;
    private bool isRetrying = false;
    private HashSet<int> collectedIndexes = new HashSet<int>();
    private BookWordPage wordPage;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        wordPage = FindFirstObjectByType<BookWordPage>(FindObjectsInactive.Include);
        if (wordPage == null) Debug.LogError("WordProgressManager: No BookWordPage found in the scene.");
    }

    private void Start()
    {
        StartNewGame();
    }

    public void RetrySameWord()
    {
        isRetrying = true;
        StartNewGame();
    }

    public void AdvanceToNextWord()
    {
        isRetrying = false;
        StartNewGame();
    }

    public void StartNewGame()
    {
        // Reset the flag at the start of every new word
        AllLettersCollected = false;

        CreatureManager.Instance.ResetCreatures();

        // Get next word
        targetWord = GetNextWord().ToUpper();
        LetterManager.Instance.ResetLettersForNewWord(targetWord);

        // Reset Player
        PlayerInventory.Instance.ResetInventory();
        PlayerHealth.Instance.ResetForNewLevel();
        wordPage.Refresh();
        collectedIndexes.Clear();
    }

    private string GetNextWord()
    {
        if (wordList.Length <= 0) return "PLACE";
        
        if (isRetrying)
        {
            return targetWord; // Keep the same word for retry
        }
        else
        {
            string nextWord = wordList[currentWordIndex];
            currentWordIndex = (currentWordIndex + 1) % wordList.Length; // Loop back to start
            return nextWord;
        }
    }

    public void CollectLetter(char collectedChar)
    {
        collectedChar = char.ToUpper(collectedChar);

        for (int i = 0; i < targetWord.Length; i++)
        {
            if (targetWord[i] == collectedChar && !collectedIndexes.Contains(i))
            {
                collectedIndexes.Add(i);
                break;
            }
        }
        
        wordPage.Refresh();
        Debug.Log("Collected Letter: " + collectedChar);

        // Once collected letters match the word
        if (collectedIndexes.Count == targetWord.Length)
        {
            AllLettersCollected = true;
            Debug.Log("All letters collected! Return the letters to the fountain.");
        }
    }

    public bool HasCollectedLetter(char c)
    {
        c = char.ToUpper(c);

        foreach (var letter in collectedLetters)
        {
            if (char.ToUpper(letter.letter) == c) return true;
        }

        return false;
    }
}