using NUnit.Framework.Internal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookWordPage : MonoBehaviour
{
    [Header("Letter Layout")]
    public TextMeshProUGUI wordText;
    public GameObject letterSlotPrefab;
    public Transform letterSlotContainer;

    private WordProgressManager wpi => WordProgressManager.Instance;

    public void Refresh()
    {
        ClearLetters();
        SetupLetterSlots();

        wordText.text = wpi.targetWord;
    }

    private void SetupLetterSlots()
    {
        char[] chars = wpi.targetWord.ToCharArray();

        // Create LetterSLotUI elements for each letter in target word and initialize them
        for (int i = 0; i < chars.Length; i++)
        {
            char letter = chars[i];
            GameObject letterSlot = Instantiate(letterSlotPrefab, letterSlotContainer);
            LetterSlotUI slot = letterSlot.GetComponent<LetterSlotUI>();
            Image slotImage = letterSlot.GetComponent<Image>();

            slot.Initialize(letter, LetterSpriteDatabase.Instance.GetUncollectedSprite(letter)); 

            // Set sprite to collected if player has the letter
            foreach (char c in wpi.collectedLetters)
            {
                if (c == letter) slot.SetSprite(LetterSpriteDatabase.Instance.GetCollectedSprite(letter));
            }

            // Set image to proper siee
            slotImage.SetNativeSize();
            letterSlot.transform.localScale = new Vector3(4, 4, 4); 
        }
    }

    private void ClearLetters()
    {
        for (int i = letterSlotContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(letterSlotContainer.GetChild(i).gameObject);
        }
    }

}
