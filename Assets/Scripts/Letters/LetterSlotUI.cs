using UnityEngine;
using UnityEngine.UI;

public class LetterSlotUI : MonoBehaviour
{
    public Image letterImage;
    private char letter;

    public void SetLetter(char c, Sprite uncollectedSprite)
    {
        letter = c;
        if (letterImage != null)
            letterImage.sprite = uncollectedSprite;
    }

    public void SetCollectedSprite(Sprite collectedSprite)
    {
        if (letterImage != null)
            letterImage.sprite = collectedSprite;
    }

    public char GetLetter()
    {
        return letter;
    }
}