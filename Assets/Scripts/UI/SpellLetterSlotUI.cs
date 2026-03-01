using UnityEngine;
using UnityEngine.EventSystems;

public class SpellLetterSlotUI : LetterDropSlotUI
{
    private BookSpellEntry parentEntry;

    public void Initialize(char required, BookSpellEntry entry)
    {
        requiredLetter = char.ToUpper(required);
        parentEntry = entry;
    }

    protected override void AcceptLetter(DraggableLetterUI letter)
    {
        base.AcceptLetter(letter);
        parentEntry.NotifySlotChanged();
    }
}