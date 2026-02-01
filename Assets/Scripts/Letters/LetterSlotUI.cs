using UnityEngine;
using UnityEngine.UI;

public class LetterSlotUI : MonoBehaviour
{
    public Image letterImage;
    public char letter { get; private set; }

    public void Initialize(char c, Sprite letterSprite)
    {
        SetLetter(c);
        SetSprite(letterSprite);
    }

    public void SetLetter(char c)
    {
        letter = c;
    }

    public void SetSprite(Sprite letterSprite)
    {
        if (letterImage != null) letterImage.sprite = letterSprite;
    }
}