using UnityEngine;

public class LetterSlot : MonoBehaviour
{
    private char letter;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetLetter(char c, Sprite sprite)
    {
        letter = c;
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }

    public char GetLetter()
    {
        return letter;
    }
}