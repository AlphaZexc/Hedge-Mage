using System.Collections.Generic;
using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public static SpellManager Instance;

    private List<SpellBase> availableSpells = new List<SpellBase>();
    private PlayerInventory inventory => PlayerInventory.Instance;
    private SpellUI spellVisuals => SpellUI.Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Initialize spells
        availableSpells.Add(new Spell_Jump());
        availableSpells.Add(new Spell_Repulse());
        availableSpells.Add(new Spell_Restore());
    }

    private void Update()
    {
        // Cast last completed spell with F
        if (Input.GetKeyDown(KeyCode.F) && inventory.lastCompletedSpell != null)
        {
            inventory.lastCompletedSpell.Cast(gameObject);
            spellVisuals.UpdateSpellUI();
        }
    }

    // Called by book UI when player clicks a spell button
    public bool TryCompleteSpell(SpellBase spell)
    {
        if (!spell.CanBeCompleted(inventory.collectedLetters))
            return false;

        // Consume letters and store last completed spell
        spell.ConsumeLetters(inventory);
        WordProgressManager.Instance.UpdateCollectedLetters();
        inventory.SetLastCompletedSpell(spell);

        spellVisuals.UpdateSpellUI(spell);

        // Regenerate missing letters for next use
        spell.GenerateMaskedSpell(2);

        Debug.Log($"Completed spell: {spell.spellName}");
        return true;
    }

    // Used by UI to display spells and button states
    public List<SpellBase> GetAvailableSpells()
    {
        return availableSpells;
    }
}
