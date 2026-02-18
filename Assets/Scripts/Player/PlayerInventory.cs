using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public List<LetterObject> collectedLetters = new List<LetterObject>();
    public SpellBase lastCompletedSpell { get; private set; }

    public bool hasItem => collectedLetters.Count > 0;
    public int LetterCount => collectedLetters.Count;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        LetterObject letterObj = other.GetComponent<LetterObject>();
        if (letterObj != null)
        {
            char collectedChar = char.ToUpper(letterObj.letter);

            collectedLetters.Add(letterObj);
            WordProgressManager.Instance.CollectLetter(collectedChar);

            // Hide letter in world
            if (letterObj.TryGetComponent(out Collider2D col)) col.enabled = false;
            if (letterObj.TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;
        }
    }

    public bool ConsumeLetter(char c)
    {
        for (int i = collectedLetters.Count - 1; i >= 0; i--)
        {
            LetterObject letter = collectedLetters[i];

            if (char.ToUpper(letter.letter) == char.ToUpper(c))
            {
                collectedLetters.RemoveAt(i);
                Debug.Log("Consumed letter: " + c);
                return true;
            }
        }

        return false;
    }

    public bool RemoveLetter(LetterObject letter)
    {
        return collectedLetters.Remove(letter);
    }

    public void SetLastCompletedSpell(SpellBase spell)
    {
        lastCompletedSpell = spell;
    }
}
