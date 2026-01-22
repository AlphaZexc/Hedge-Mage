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
    private const int BESTIARY_PAGE_INDEX = 2;

    [Header("GameObjects")]
    public List<GameObject> pages;
    public List<GameObject> tabs;
    public List<int> tabPageNumbers; // Page number corresponding to each tab in numerical order
    public Transform tabPositionLeft;
    public Transform tabPositionRight;

    [Header("Creature Bestiary Data")]
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

        if (tabs.Count == tabPageNumbers.Count && tabs.Count > 0)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int tabIndex = i; // capture a copy
                Button tabButton = tabs[i].GetComponent<Button>();
                tabButton.onClick.AddListener(() => GoToPage(tabPageNumbers[tabIndex]));
            }

        }
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
        for (int i = tabPageNumbers[BESTIARY_PAGE_INDEX]; i < pages.Count; i++)
        {
            BookPage pageComponent = pages[i].GetComponent<BookPage>();
            
            // The first creature entry corresponds to the second page in the book, etc.
            int bestiaryIndex = i - tabPageNumbers[BESTIARY_PAGE_INDEX];

            if (pageComponent != null && bestiaryIndex < bestiaryEntries.Count)
            {
                pageComponent.PopulatePage(bestiaryEntries[bestiaryIndex]);
            }
        }
    }
    
    public void GoToPage(int page)
    {
        if (page >= 0 && page < pages.Count)
        ShowPage(page);
    }

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

        for (int i = 0; i < tabs.Count; i++)
        {
            if (currentPageIndex > tabPageNumbers[i])
            {
                tabs[i].transform.position = new Vector3(tabPositionLeft.position.x, tabs[i].transform.position.y, tabs[i].transform.position.z);
            } else
            {
                tabs[i].transform.position = new Vector3(tabPositionRight.position.x, tabs[i].transform.position.y, tabs[i].transform.position.z);
            }
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