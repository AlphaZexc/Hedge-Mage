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
    public bool AllLettersCollected { get; private set; }
    public List<LetterObject> collectedLetters => PlayerInventory.Instance.collectedLetters;

    [SerializeField] private string[] wordList = { "APPLE", "HOUSE", "LIGHT", "BRICK", "WATER" };
    private int currentWordIndex = 0;
    private bool isRetrying = false;

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
        AllLettersCollected = false;
        CreatureManager.Instance.ResetCreatures();

        targetWord = GetNextWord().ToUpper();
        LetterManager.Instance.ResetLettersForNewWord(targetWord);

        PlayerInventory.Instance.ResetInventory();
        PlayerHealth.Instance.ResetForNewLevel();
        wordPage.Refresh();
    }

    private string GetNextWord()
    {
        if (wordList.Length <= 0) return "PLACE";

        if (isRetrying)
            return targetWord;

        string nextWord = wordList[currentWordIndex];
        currentWordIndex = (currentWordIndex + 1) % wordList.Length;
        return nextWord;
    }

    public void CollectLetter(char collectedChar)
    {
        collectedChar = char.ToUpper(collectedChar);

        wordPage.Refresh();
        Debug.Log("Collected Letter: " + collectedChar);

        if (CheckAllLettersCollected())
        {
            AllLettersCollected = true;
            Debug.Log("All letters collected! Return the letters to the fountain.");
        }
    }

    // Checks whether all needed letters have been collected, checks frequency as well
    private bool CheckAllLettersCollected()
    {
        // Tally how many of each letter the target word requires
        Dictionary<char, int> required = new Dictionary<char, int>();
        foreach (char c in targetWord)
        {
            if (!required.ContainsKey(c)) required[c] = 0;
            required[c]++;
        }

        // Subtract what the player has collected
        foreach (LetterObject letter in collectedLetters)
        {
            char c = char.ToUpper(letter.letter);
            if (required.ContainsKey(c))
                required[c]--;
        }

        // All requirements met if no letter count is still positive
        foreach (int remaining in required.Values)
        {
            if (remaining > 0) return false;
        }

        return true;
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