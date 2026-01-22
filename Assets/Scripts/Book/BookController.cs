using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// The BestiaryEntry class is unchanged
[System.Serializable]
public class BestiaryEntry
{
    public string creatureName;
    public Sprite leftPageImage;
    public Sprite rightPageImage;
}

public class BookController : MonoBehaviour
{
    [Header("Page GameObjects")]
    [Tooltip("The parent GameObjects for each page. Drag them here in the order you want them to appear.")]
    public List<GameObject> pages;

    [Header("Creature Bestiary Data")]
    [Tooltip("The data for your creature pages. This must match the order of your creature pages in the list above.")]
    public List<BestiaryEntry> bestiaryEntries;

    [Header("Navigation")]
    public Button nextButton;
    public Button prevButton;
    public TMP_Text pageNumberText; 

    private int currentPageIndex = 0;

    private void Awake()
    {
        nextButton.onClick.AddListener(GoToNextPage);
        prevButton.onClick.AddListener(GoToPreviousPage);
    }

    private void Start()
    {
        // --- NEW: Populate all the pages with their data at the start ---
        PopulateAllPages();
        // Then, show the first page.
        ShowPage(0);
    }

    // This new method sets up all the creature pages with their correct sprites once.
    private void PopulateAllPages()
    {
        // We start at index 1, because page 0 is the Word Status page and doesn't need this data.
        for (int i = 1; i < pages.Count; i++)
        {
            BookPage pageComponent = pages[i].GetComponent<BookPage>();
            
            // The first creature entry corresponds to the second page in the book, etc.
            int bestiaryIndex = i - 1;

            if (pageComponent != null && bestiaryIndex < bestiaryEntries.Count)
            {
                pageComponent.PopulatePage(bestiaryEntries[bestiaryIndex]);
            }
        }
    }
    
    // The OnEnable method is no longer needed, Start handles the initial setup.

    public void GoToNextPage()
    {
        if (currentPageIndex < pages.Count - 1)
        {
            ShowPage(currentPageIndex + 1);
        }
    }

    public void GoToPreviousPage()
    {
        if (currentPageIndex > 0)
        {
            ShowPage(currentPageIndex - 1);
        }
    }

    private void ShowPage(int index)
    {
        currentPageIndex = index;

        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].SetActive(i == currentPageIndex);
        }

        // Refresh word page if this page has one
        BookWordPage wordPage = pages[currentPageIndex].GetComponent<BookWordPage>();
        if (wordPage != null)
        {
            wordPage.Refresh();
        }

        UpdateNavigationButtons();
    }



    private void UpdateNavigationButtons()
    {
        prevButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < pages.Count - 1);
        
        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex + 1} / {pages.Count}";
        }
    }
}