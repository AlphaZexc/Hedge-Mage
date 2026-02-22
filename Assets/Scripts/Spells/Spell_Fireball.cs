using UnityEngine;

public class Spell_Fireball : SpellBase
{
    private GameObject fireballPrefab;
    private Transform firePoint;
    public Spell_Fireball(GameObject fireballPrefab, Transform firePoint)
    {
        spellName = "FIREBALL";
        GenerateMaskedSpell(NUMBER_MASKED_CHARS);

        spellType = SpellType.Attack;

        this.fireballPrefab = fireballPrefab;
        this.firePoint = firePoint; 
    }

    public override void Cast(GameObject player)
    {
        base.Cast(player);

        Debug.Log("Fireball cast!");
        PlayerSpell playerSpell = player.GetComponent<PlayerSpell>();
        playerSpell.CastFireball(fireballPrefab, firePoint);
    }
}
