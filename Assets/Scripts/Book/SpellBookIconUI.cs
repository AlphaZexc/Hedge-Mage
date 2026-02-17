using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static SpellBase;

public class SpellBookIconUI : MonoBehaviour
{
    public Image bookImage;

    [System.Serializable]
    public class SpellBookSpriteSet
    {
        public SpellType type;
        public Sprite open;
        public Sprite half;
        public Sprite closed;
    }

    public List<SpellBookSpriteSet> spriteSets = new List<SpellBookSpriteSet>();

    SpellBase currentSpell;
    Dictionary<SpellType, SpellBookSpriteSet> lookup;

    void Awake()
    {
        lookup = new Dictionary<SpellType, SpellBookSpriteSet>();
        foreach (var set in spriteSets)
            lookup[set.type] = set;
    }

    public void SetSpell(SpellBase spell)
    {
        currentSpell = spell;

        if (spell == null)
        {
            bookImage.enabled = false;
            return;
        }

        bookImage.enabled = true;
        RefreshSprite();
    }

    void Update()
    {
        if (currentSpell == null) return;
        RefreshSprite();
    }

    void RefreshSprite()
    {
        if (!lookup.ContainsKey(currentSpell.spellType))
            return;

        var set = lookup[currentSpell.spellType];
        float p = currentSpell.CooldownPercent;

        if (p <= 0f)
            bookImage.sprite = set.open;
        else if (p >= 0.5f)
            bookImage.sprite = set.closed;
        else
            bookImage.sprite = set.half;
    }
}
