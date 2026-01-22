using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookWordPage : MonoBehaviour
{
    [Header("Letter Layout")]
    public GameObject letterImagePrefab; // Image-only prefab
    public TextMeshProUGUI wordText;
    public Transform letterContainer;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (WordProgressManager.Instance == null ||
            LetterSpriteDatabase.Instance == null)
            return;

        ClearLetters();

        string word = WordProgressManager.Instance.GetTargetWord();

        wordText.text = word;
    }

    private void ClearLetters()
    {
        for (int i = letterContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(letterContainer.GetChild(i).gameObject);
        }
    }
}
