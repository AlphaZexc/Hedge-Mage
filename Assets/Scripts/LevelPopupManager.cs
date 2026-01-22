using UnityEngine;
using TMPro;

public class LevelPopupManager : MonoBehaviour
{
    public static LevelPopupManager Instance;

    [Header("Popup References")]
    public GameObject levelFailPopup;
    public GameObject levelCompletePopup;
    public GameObject bookPopup;
    public GameObject buttonBook;

    [Header("Time Display Texts")]
    public TMP_Text failTimeText;
    public TMP_Text completeTimeText;

    private string finalTimeFormatted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowLevelFailPopup(float finalTime)
    {
        FormatAndStoreTime(finalTime);
        if (failTimeText != null)
            failTimeText.text = finalTimeFormatted;

        levelFailPopup?.SetActive(true);
        HideBookButton();
        Time.timeScale = 0f;
    }

    public void ShowLevelCompletePopup(float finalTime)
    {
        FormatAndStoreTime(finalTime);
        if (completeTimeText != null)
            completeTimeText.text = finalTimeFormatted;

        levelCompletePopup?.SetActive(true);
        HideBookButton();
        Time.timeScale = 0f;
    }

    private void FormatAndStoreTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60F);
        finalTimeFormatted = $"{minutes:00}:{seconds:00}";
    }

    public void ShowBookPopup()
    {
        bookPopup?.SetActive(true);
        HideBookButton();
        Time.timeScale = 0f;
    }

    public void CloseBookPopup()
    {
        bookPopup?.SetActive(false);
        ShowBookButton();
        Time.timeScale = 1f;
    }

    public void OnClickRestartSameWord()
    {
        Debug.Log("Restart button clicked. Generating same word.");
        
        // Hide popups
        if (levelFailPopup != null) levelFailPopup.SetActive(false);
        if (levelCompletePopup != null) levelCompletePopup.SetActive(false);
        if (bookPopup != null) bookPopup.SetActive(false);

        PlayerHealth.Instance.ResetForNewLevel();
        CreatureManager.Instance.ResetCreatures();
        WordProgressManager.Instance.RetrySameWord();

        Time.timeScale = 1f;
    }

    public void OnClickAdvanceToNextWord()
    {
        Debug.Log("Next level button clicked. Generating next word.");
        
        // Hide popups
        if (levelFailPopup != null) levelFailPopup.SetActive(false);
        if (levelCompletePopup != null) levelCompletePopup.SetActive(false);
        if (bookPopup != null) bookPopup.SetActive(false);

        PlayerHealth.Instance.ResetForNewLevel();
        CreatureManager.Instance.ResetCreatures();
        WordProgressManager.Instance.AdvanceToNextWord();

        Time.timeScale = 1f;
    }


    private void HideBookButton()
    {
        if (buttonBook != null) buttonBook.SetActive(false);
    }

    private void ShowBookButton()
    {
        if (buttonBook != null) buttonBook.SetActive(true);
    }
}






// using UnityEngine;
// using TMPro;


// public class LevelPopupManager : MonoBehaviour
// {
//     public static LevelPopupManager Instance;

//     [Header("Popup References")]
//     public GameObject levelFailPopup;
//     public GameObject levelCompletePopup;
//     public GameObject bookPopup;
//     public GameObject buttonBook;

//     [Header("Time Display Texts")]
//     public TMP_Text failTimeText;      // assign the text in LevelFailPopup
//     public TMP_Text completeTimeText;  //  assign the text in LevelCompletePopup

//     private string finalTimeFormatted;

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     public void ShowLevelFailPopup(float finalTime)
//     {
//         if (levelFailPopup != null)
//         {
//             FormatAndStoreTime(finalTime);
//             if (failTimeText != null)
//                 failTimeText.text = finalTimeFormatted;

//             levelFailPopup.SetActive(true);
//             HideBookButton();
//             Time.timeScale = 0f; // Pause time
//         }
//     }

//     public void ShowLevelCompletePopup(float finalTime)
//     {
//         if (levelCompletePopup != null)
//         {
//             FormatAndStoreTime(finalTime);
//             if (completeTimeText != null)
//                 completeTimeText.text = finalTimeFormatted;

//             levelCompletePopup.SetActive(true);
//             HideBookButton();
//             Time.timeScale = 0f; // Pause time
//         }
//     }

//     private void FormatAndStoreTime(float timeInSeconds)
//     {
//         int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
//         int seconds = Mathf.FloorToInt(timeInSeconds % 60F);
//         finalTimeFormatted = $"{minutes:00}:{seconds:00}";
//     }

//     public void ShowBookPopup()
//     {
//         if (bookPopup != null)
//         {
//             bookPopup.SetActive(true);
//             HideBookButton();
//             Time.timeScale = 0f; // Pause time
//         }
//     }

//     public void CloseBookPopup()
//     {
//         if (bookPopup != null)
//         {
//             bookPopup.SetActive(false);
//             ShowBookButton();
//             Time.timeScale = 1f; // Resume time
//         }
//     }

//     public void OnClickRestartSameWord()
//     {
//         Debug.Log("Restart button clicked. Generating same word.");

//         // Reset Player Health and Lives (including the timer)
//         PlayerHealth.Instance.ResetForNewLevel();

//         // Reset Creatures
//         CreatureManager.Instance.ResetCreatures();

//         Time.timeScale = 1f; // Resume game time
//         WordProgressManager.Instance.RetrySameWord();
//     }

//     public void OnClickAdvanceToNextWord()
//     {
//         Debug.Log("Next level button clicked. Generating next word.");

//         // Reset Player Health and Lives (including the timer)
//         PlayerHealth.Instance.ResetForNewLevel();

//         // Reset Creatures
//         CreatureManager.Instance.ResetCreatures();

//         Time.timeScale = 1f; // Resume game time
//         WordProgressManager.Instance.AdvanceToNextWord();
//     }


//     private void HideBookButton()
//     {
//         if (buttonBook != null)
//         {
//             buttonBook.SetActive(false);
//         }
//     }

//     private void ShowBookButton()
//     {
//         if (buttonBook != null)
//         {
//             buttonBook.SetActive(true);
//         }
//     }
// }


// public void OnClickRestartSameWord() // RestartLevel() old code
    // {
    //     Debug.Log("Restart button clicked. Generating same word.");

    //     // Reset Player Health and Lives
    //     PlayerHealth.Instance.ResetForNewLevel();
        
    //     // Reset the Timer
    //     // Assuming you have a reference to the timer UI that you want to reset to 00:00
    //     // (If you’re using a script like WordProgressManager to handle the timer, reset there)
    //     if (WordProgressManager.Instance != null)
    //     {
    //         WordProgressManager.Instance.ResetTimer(); // Example, implement if needed
    //     }

    //     // Reset Creatures
    //     CreatureManager.Instance.ResetCreatures();

    //     Time.timeScale = 1f; // Resume game time
    //     WordProgressManager.Instance.RetrySameWord();
    // }

    // public void OnClickAdvanceToNextWord() // StartNextLevel() old code
    // {
    //     Debug.Log("Next level button clicked. Generating next word.");

    //     // Reset Player Health and Lives
    //     PlayerHealth.Instance.ResetForNewLevel();

    //     // Reset the Timer
    //     if (WordProgressManager.Instance != null)
    //     {
    //         WordProgressManager.Instance.ResetTimer(); // Example, implement if needed
    //     }

    //     // Reset Creatures
    //     CreatureManager.Instance.ResetCreatures();

    //     Time.timeScale = 1f; // Resume game time
    //     WordProgressManager.Instance.AdvanceToNextWord();
    // }