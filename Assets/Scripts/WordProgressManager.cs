using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordProgressManager : MonoBehaviour
{
    public static WordProgressManager Instance;

    [Header("Popups")]
    public GameObject bookPopup;
    public GameObject levelCompletePopup;
    public GameObject levelFailPopup;

    [Header("Letter/Word Information")]
    public string targetWord { get; private set; }
    public bool AllLettersCollected { get; private set; } // Word is complete
    public TextMeshProUGUI currentLetterText;
    public BookWordPage wordPage;
    public List<char> collectedLetters = new List<char>();

    [SerializeField] private string[] wordList = { "APPLE", "HOUSE", "LIGHT", "BRICK", "WATER" };

    private int currentWordIndex = 0;
    private bool isRetrying = false;
    private HashSet<int> collectedIndexes = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentLetterText.text = "";
    }

    private void Start()
    {
        StartNewGame();
    }

    private void Update()
    {
        string line = "";
        foreach (char c in collectedLetters)
        {
            line += c + " ";
        }
        Debug.Log(line);
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

        targetWord = GetNextWord().ToUpper();
        collectedIndexes.Clear();

        LetterManager.Instance.ResetLettersForNewWord(targetWord);
    }

    private string GetNextWord()
    {
        if (wordList.Length == 0) return "PLACE";
        return isRetrying ? wordList[(currentWordIndex - 1 + wordList.Length) % wordList.Length] : wordList[currentWordIndex++ % wordList.Length];
    }

    public void CollectLetter(char collectedChar)
    {
        collectedLetters.Clear();
        foreach (var letterObj in PlayerInventory.Instance.collectedLetters)
        {
            collectedLetters.Add(letterObj.letter);
        }

        collectedChar = char.ToUpper(collectedChar);

        for (int i = 0; i < targetWord.Length; i++)
        {
            if (targetWord[i] == collectedChar && !collectedIndexes.Contains(i))
            {
                collectedIndexes.Add(i);
                break;
            }
        }
        
        UpdateCollectedLetters();
        wordPage.Refresh();
        Debug.Log("Collected Letter: " + collectedChar);

        // Once collected letters match the word
        if (collectedIndexes.Count == targetWord.Length)
        {
            AllLettersCollected = true;
            Debug.Log("All letters collected! Return the letters to the fountain.");
        }
    }

    // Updates the collected letters in the Book
    public void UpdateCollectedLetters()
    {
        currentLetterText.text = "";
        foreach (var letter in collectedLetters)
        {
            currentLetterText.text += letter.ToString() + " ";
        }
    }

    public bool IsLetterCollected(char c)
    {
        c = char.ToUpper(c);
        for (int i = 0; i < targetWord.Length; i++)
        {
            if (targetWord[i] == c && collectedIndexes.Contains(i)) return true;
        }
        return false;
    }
}