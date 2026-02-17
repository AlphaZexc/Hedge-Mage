using UnityEngine;

public class Spell_Fireball : SpellBase
{
    public Spell_Fireball()
    {
        spellName = "FIREBALL";
        GenerateMaskedSpell(NUMBER_MASKED_CHARS);

        spellType = SpellType.Attack;
    }

    public override void Cast(GameObject player)
    {
        base.Cast(player);
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.maxLives += 1;

            Debug.Log("Fireball cast!");
        }
    }
}
