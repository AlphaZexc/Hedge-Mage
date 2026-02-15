using UnityEngine;

public class Spell_Fireball : SpellBase
{
    public Spell_Fireball()
    {
        spellName = "FIREBALL";
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
