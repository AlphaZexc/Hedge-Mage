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
    private bool isBookOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        CloseBookPopup();
    }

    private void Update()
    {
        // Do NOT allow book during tutorial or end states
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleBookPopup();
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
    public void ToggleBookPopup()
    {
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();

        isBookOpen = !isBookOpen;
        playerMovement.SetMovementEnabled(!isBookOpen);

        if (isBookOpen)
        {
            ShowBookPopup();
        }
        else
        {
            CloseBookPopup();
        }
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
        ShowBookButton();

        WordProgressManager.Instance.RetrySameWord();

        Time.timeScale = 1f;
    }

    public void OnClickAdvanceToNextWord()
    {
        Debug.Log("Next level button clicked. Generating next word.");
        
        // Hide popups
        if (levelFailPopup != null) levelFailPopup.SetActive(false);
        if (levelCompletePopup != null) levelCompletePopup.SetActive(false);
        ShowBookButton();

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