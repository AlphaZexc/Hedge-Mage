using UnityEngine;
using UnityEngine.UI;

public class LetterSlotUI : MonoBehaviour
{
    public Image letterImage;
    public char letter { get; private set; }

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
}