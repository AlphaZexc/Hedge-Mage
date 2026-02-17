using UnityEngine;

public class Spell_Jump : SpellBase
{
    public Spell_Jump()
    {
        spellName = "JUMP";
        GenerateMaskedSpell(NUMBER_MASKED_CHARS);

        spellType = SpellType.Mobility;
    }

    public override void Cast(GameObject player)
    {
        base.Cast(player);
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(Vector2.up * 400f);
        }

        Debug.Log("Jump spell cast!");
    }
}
