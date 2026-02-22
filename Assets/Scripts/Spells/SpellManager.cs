using System.Collections.Generic;
using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public static SpellManager Instance;

    private List<SpellBase> availableSpells = new List<SpellBase>();
    private PlayerInventory inventory => PlayerInventory.Instance;
    private SpellUI spellVisuals => SpellUI.Instance;

    [Header("Fireball")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Initialize spells
        availableSpells.Add(new Spell_Jump());
        availableSpells.Add(new Spell_Fireball(fireballPrefab, firePoint));
        availableSpells.Add(new Spell_Restore());
    }
    private void Update()
    {
        foreach (var spell in availableSpells)
            spell.TickCooldown(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.F) && inventory.lastCompletedSpell != null)
        {
            Debug.Log("F pressed");

            if (inventory.lastCompletedSpell == null)
                Debug.Log("Last spell is NULL");
            else
                Debug.Log("Last spell is " + inventory.lastCompletedSpell.spellName);

            Debug.Log($"Cooldown: {inventory.lastCompletedSpell.CooldownPercent}"); 

            if (!inventory.lastCompletedSpell.IsOnCooldown)
            {
                inventory.lastCompletedSpell.Cast(inventory.gameObject);
                spellVisuals.UpdateSpellUI(inventory.lastCompletedSpell);
            }
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

        spell.ResetCooldown();

        Debug.Log($"Completed spell: {spell.spellName}");
        return true;
    }

    // Used by UI to display spells and button states
    public List<SpellBase> GetAvailableSpells()
    {
        return availableSpells;
    }
}
