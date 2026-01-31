using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookSpellEntry : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI spellText;
    [SerializeField] private Button completeButton;

    [HideInInspector] public SpellBase spell;

    private BookSpellPage parentPage;

    // Initialize the UI row
    public void Initialize(SpellBase spell, BookSpellPage page)
    {
        this.spell = spell;
        parentPage = page;

        spellText.text = spell.GetMaskedSpell();

        completeButton.onClick.RemoveAllListeners();
        completeButton.onClick.AddListener(OnButtonClicked);
    }

    // Enable or disable based on player inventory
    public void UpdateState(bool canComplete)
    {
        spellText.text = spell.GetMaskedSpell();
        completeButton.interactable = canComplete;
    }

    private void OnButtonClicked()
    {
        parentPage.OnSpellButtonPressed(spell);
    }
}
