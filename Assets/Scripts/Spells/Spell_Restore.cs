using UnityEngine;

public class Spell_Restore : SpellBase
{
    public Spell_Restore()
    {
        spellName = "RESTORE";
        GenerateMaskedSpell(NUMBER_MASKED_CHARS);
    }

    public override void Cast(GameObject player)
    {
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null) 
        {
            playerHealth.maxLives += 1;

            Debug.Log("Restore cast!");
        }
    }
}
