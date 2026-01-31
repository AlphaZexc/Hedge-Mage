using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordProgressManager : MonoBehaviour
{
    public static WordProgressManager Instance;

    public GameObject letterSlotPrefab;
    public Transform slotContainer;

    [SerializeField] private string[] wordList = { "APPLE", "HOUSE", "LIGHT", "BRICK", "WATER" };
    private string targetWord;
    private int currentWordIndex = 0;
    private List<LetterSlotUI> letterSlots = new List<LetterSlotUI>();

    private bool isRetrying = false;

    public GameObject bookPopup;
    public GameObject levelCompletePopup;
    public GameObject levelFailPopup;

    public TextMeshProUGUI currentLetterText;
    private List<char> collectedLetters = new List<char>();
    public HashSet<int> collectedIndexes = new HashSet<int>();

    public bool AllLettersCollected { get; private set; } // Word is complete

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
        SetupUISlots();

        LetterManager.Instance.ResetLettersForNewWord(targetWord);
    }

    private string GetNextWord()
    {
        if (wordList.Length == 0) return "PLACE";
        return isRetrying ? wordList[(currentWordIndex - 1 + wordList.Length) % wordList.Length] : wordList[currentWordIndex++ % wordList.Length];
    }

    private void SetupUISlots()
    {
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        letterSlots.Clear();

        foreach (char c in targetWord)
        {
            GameObject slotObj = Instantiate(letterSlotPrefab, slotContainer);
            LetterSlotUI slot = slotObj.GetComponent<LetterSlotUI>();
            slot.SetLetter(c, LetterSpriteDatabase.Instance.GetUncollectedSprite(c));
            letterSlots.Add(slot);
        }
    }

    public void CollectLetter(char collectedChar)
    {
        collectedChar = char.ToUpper(collectedChar);
        collectedLetters.Add(collectedChar);

        for (int i = 0; i < targetWord.Length; i++)
        {
            if (targetWord[i] == collectedChar && !collectedIndexes.Contains(i))
            {
                collectedIndexes.Add(i);
                break;
            }
        }
        
        UpdateCollectedLetters();
        UpdateSlots();

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
        collectedLetters.Clear();
        foreach (var letterObj in PlayerInventory.Instance.collectedLetters)
        {
            collectedLetters.Add(letterObj.letter);
        }

        currentLetterText.text = "";
        foreach (var letter in collectedLetters)
        {
            currentLetterText.text += letter.ToString() + " ";
        }
    }

    private void UpdateSlots()
    {
        for (int i = 0; i < targetWord.Length; i++)
        {
            char letter = targetWord[i];
            Sprite sprite = collectedIndexes.Contains(i)
                ? LetterSpriteDatabase.Instance.GetCollectedSprite(letter)
                : LetterSpriteDatabase.Instance.GetUncollectedSprite(letter);

            letterSlots[i].SetCollectedSprite(sprite);
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

    public string GetTargetWord() => targetWord;
}