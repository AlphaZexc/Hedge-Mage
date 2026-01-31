using System.Collections.Generic;
using UnityEngine;

public abstract class SpellBase
{
    protected const int NUMBER_MASKED_CHARS = 2;

    public string spellName;              // Full spell word (e.g. "JUMP")
    protected string maskedSpell;          // Masked version (e.g. "J_M_")
    protected HashSet<char> missingLetters = new HashSet<char>();

    // Generate a masked version with random missing letters
    public void GenerateMaskedSpell(int missingCount)
    {
        maskedSpell = spellName;
        missingLetters.Clear();

        List<int> availableIndexes = new List<int>();
        for (int i = 0; i < spellName.Length; i++)
            availableIndexes.Add(i);

        // Randomly remove letters
        for (int i = 0; i < missingCount && availableIndexes.Count > 0; i++)
        {
            int index = Random.Range(0, availableIndexes.Count);
            int letterIndex = availableIndexes[index];
            availableIndexes.RemoveAt(index);

            char missingChar = spellName[letterIndex];
            missingLetters.Add(missingChar);

            maskedSpell = maskedSpell.Remove(letterIndex, 1).Insert(letterIndex, "_");
        }
    }

    // Check if player inventory contains all missing letters
    public bool CanBeCompleted(List<LetterObject> playerLetters)
    {
        HashSet<char> inventoryChars = new HashSet<char>();
        foreach (var letter in playerLetters)
            inventoryChars.Add(char.ToUpper(letter.letter));

        foreach (char c in missingLetters)
        {
            if (!inventoryChars.Contains(c))
                return false;
        }

        return true;
    }

    // Consume the required letters from inventory
    public void ConsumeLetters(PlayerInventory inventory)
    {
        foreach (char c in missingLetters)
        {
            inventory.ConsumeLetter(c);
        }
    }


    public string GetMaskedSpell() => maskedSpell;

    // What happens when the spell is cast
    public abstract void Cast(GameObject player);
}
