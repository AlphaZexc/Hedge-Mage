using UnityEngine;

public class Spell_Repulse : SpellBase
{
    public Spell_Repulse()
    {
        spellName = "REPULSE";
        GenerateMaskedSpell(NUMBER_MASKED_CHARS);
    }

    public override void Cast(GameObject player)
    {
        // Example: push nearby objects away
        Collider2D[] hits = Physics2D.OverlapCircleAll(player.transform.position, 3f);
        foreach (var hit in hits)
        {
            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null && hit.gameObject != player)
            {
                Vector2 dir = (hit.transform.position - player.transform.position).normalized;
                rb.AddForce(dir * 300f);
            }
        }

        Debug.Log("Repulse spell cast!");
    }
}
