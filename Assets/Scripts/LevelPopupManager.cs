using UnityEngine;
using TMPro;
using BookCurlPro;

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

        CloseBookPopup();
    }

    private void Update()
    {
        // Opens/closes book when escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (bookPopup.activeInHierarchy) CloseBookPopup();
            else ShowBookPopup();
        }
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
        bookPopup.SetActive(true);
        HideBookButton();
    }

    public void CloseBookPopup()
    {
        bookPopup.SetActive(false);
        ShowBookButton();
    }

    public void OnClickRestartSameWord()
    {
        Debug.Log("Restart button clicked. Generating same word.");
        
        // Hide popups
        if (levelFailPopup != null) levelFailPopup.SetActive(false);
        if (levelCompletePopup != null) levelCompletePopup.SetActive(false);

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