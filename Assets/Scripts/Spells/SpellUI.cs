using TMPro;
using UnityEngine;

public class SpellUI : MonoBehaviour
{
    public static SpellUI Instance;

    public SpellBookIconUI bookIcon;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateSpellUI(SpellBase spell = null)
    {
        if (spell != null)
        {
            bookIcon.SetSpell(spell);
        }
        else
        {
            bookIcon.SetSpell(null);
        }
    }
}
