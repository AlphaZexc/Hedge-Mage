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
    public List<char> collectedLetters = new List<char>();
    public HashSet<int> collectedIndexes = new HashSet<int>();

    public bool AllLettersCollected { get; private set; } // Word is complete

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
        // --- NEW: Reset the flag at the start of every new word. ---
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

    public void CollectLetter(char collected)
    {
        collected = char.ToUpper(collected);
        collectedLetters.Add(collected);

        for (int i = 0; i < targetWord.Length; i++)
        {
            if (targetWord[i] == collected && !collectedIndexes.Contains(i))
            {
                collectedIndexes.Add(i);
                break;
            }
        }

        currentLetterText.text = "";
        foreach (var letter in collectedLetters)
        {
            currentLetterText.text += letter.ToString() + " ";
        }

        UpdateSlots();

        if (collectedIndexes.Count == targetWord.Length)
        {
            // --- NEW: Instead, we set a flag and notify the player. ---
            AllLettersCollected = true;
            Debug.Log("All letters collected! Return the letters to the fountain.");
            // You could also trigger a UI notification or sound effect here.
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



// Hidden 8/5/25
// using System.Collections.Generic;
// using UnityEngine;

// public class WordProgressManager : MonoBehaviour
// {
//     public static WordProgressManager Instance;

//     public GameObject letterSlotPrefab;
//     public Transform slotContainer;

//     [SerializeField] private string[] wordList = { "APPLE", "HOUSE", "LIGHT", "BRICK", "WATER" };
//     private string targetWord;
//     private int currentWordIndex = 0;
//     private HashSet<int> collectedIndexes = new HashSet<int>();
//     private List<LetterSlotUI> letterSlots = new List<LetterSlotUI>();

//     public GameObject bookPopup;
//     public GameObject levelCompletePopup;
//     public GameObject levelFailPopup;

//     private bool isRetrying = false;

//     private void Awake()
//     {
//         if (Instance == null) Instance = this;
//         else Destroy(gameObject);
//     }

//     private void Start()
//     {
//         StartNewGame();
//     }

//     public void RetrySameWord()
//     {
//         isRetrying = true;
//         StartNewGame();
//     }

//     public void AdvanceToNextWord()
//     {
//         isRetrying = false;
//         StartNewGame();
//     }

//     public void StartNewGame()
//     {
//         targetWord = GetNextWord().ToUpper();
//         collectedIndexes.Clear();
//         SetupUISlots();

//         LetterManager.Instance.ResetLettersForNewWord(targetWord);  // THIS IS NEWLY INSERTED CODE 5/2

//     }

//     private string GetNextWord()
//     {
//         if (wordList.Length == 0) return "PLACE";
//         return isRetrying ? wordList[(currentWordIndex - 1 + wordList.Length) % wordList.Length] : wordList[currentWordIndex++ % wordList.Length];
//     }

//     private void SetupUISlots()
//     {
//         foreach (Transform child in slotContainer)
//         {
//             Destroy(child.gameObject);
//         }
//         letterSlots.Clear();

//         foreach (char c in targetWord)
//         {
//             GameObject slotObj = Instantiate(letterSlotPrefab, slotContainer);
//             LetterSlotUI slot = slotObj.GetComponent<LetterSlotUI>();
//             slot.SetLetter(c, LetterSpriteDatabase.Instance.GetUncollectedSprite(c));
//             letterSlots.Add(slot);
//         }
//     }

//     public void CollectLetter(char collected)
//     {
//         collected = char.ToUpper(collected);
//         for (int i = 0; i < targetWord.Length; i++)
//         {
//             if (targetWord[i] == collected && !collectedIndexes.Contains(i))
//             {
//                 collectedIndexes.Add(i);
//                 break;
//             }
//         }

//         UpdateSlots();

//         if (collectedIndexes.Count == targetWord.Length)
//         {
//             float finalTime = PlayerHealth.Instance.GetElapsedLevelTime();
//             LevelPopupManager.Instance.ShowLevelCompletePopup(finalTime);

//         }
//     }

//     private void UpdateSlots()
//     {
//         for (int i = 0; i < targetWord.Length; i++)
//         {
//             char letter = targetWord[i];
//             Sprite sprite = collectedIndexes.Contains(i)
//                 ? LetterSpriteDatabase.Instance.GetCollectedSprite(letter)
//                 : LetterSpriteDatabase.Instance.GetUncollectedSprite(letter);

//             letterSlots[i].SetCollectedSprite(sprite);
//         }
//     }

//     public bool IsLetterCollected(char c)
//     {
//         c = char.ToUpper(c);
//         for (int i = 0; i < targetWord.Length; i++)
//         {
//             if (targetWord[i] == c && collectedIndexes.Contains(i)) return true;
//         }
//         return false;
//     }

//     public string GetTargetWord() => targetWord;
// }