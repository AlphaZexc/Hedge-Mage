using TMPro;
using UnityEngine;

public class SpellUI : MonoBehaviour
{
    public static SpellUI Instance;

    public TextMeshProUGUI currentSpellText;
    public SpellBookIconUI bookIcon;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentSpellText.text = string.Empty;
    }

    public void UpdateSpellUI(SpellBase spell = null)
    {
        if (spell != null)
        {
            currentSpellText.text = spell.spellName;
            bookIcon.SetSpell(spell);
        }
        else
        {
            currentSpellText.text = string.Empty;
            bookIcon.SetSpell(null);
        }
    }
}
