using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookSpellPage : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform spellEntryContainer;
    [SerializeField] private GameObject spellEntryPrefab;

    private List<BookSpellEntry> spellEntries = new List<BookSpellEntry>();

    private void OnEnable()
    {
        BuildSpellList();
    }

    // Creates UI entries for each available spell
    private void BuildSpellList()
    {
        ClearExistingEntries();

        List<SpellBase> spells = SpellManager.Instance.GetAvailableSpells();

        foreach (SpellBase spell in spells)
        {
            GameObject entryObj = Instantiate(spellEntryPrefab, spellEntryContainer);
            BookSpellEntry entryUI = entryObj.GetComponent<BookSpellEntry>();

            entryUI.Initialize(spell, this);
            spellEntries.Add(entryUI);
        }

        RefreshSpellStates();
    }

    // Updates button interactability and masked text
    public void RefreshSpellStates()
    {
        List<LetterObject> playerLetters = PlayerInventory.Instance.collectedLetters;

        foreach (BookSpellEntry entry in spellEntries)
        {
            bool canComplete = entry.spell.CanBeCompleted(playerLetters);
            entry.UpdateState(canComplete);
        }
    }

    // Called by BookSpellEntry when a spell button is clicked
    public void OnSpellButtonPressed(SpellBase spell, BookSpellEntry bse)
    {
        bool success = SpellManager.Instance.TryCompleteSpell(spell, bse);
        Debug.Log($"Tried to complete spell: {spell.spellName} with a result of '{success}'");

        if (success)
        {
            RefreshSpellStates();
        }
    }

    private void ClearExistingEntries()
    {
        foreach (Transform child in spellEntryContainer)
        {
            Destroy(child.gameObject);
        }

        spellEntries.Clear();
    }
}
