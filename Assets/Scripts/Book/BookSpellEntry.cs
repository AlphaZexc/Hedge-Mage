using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookSpellEntry : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform letterContainer;
    [SerializeField] private GameObject letterTextPrefab;   // TMP text prefab
    [SerializeField] private SpellLetterSlotUI slotPrefab;  // Drop slot prefab
    [SerializeField] private Button completeButton;

    [HideInInspector] public SpellBase spell;

    private BookSpellPage parentPage;
    private List<SpellLetterSlotUI> activeSlots = new List<SpellLetterSlotUI>();

    public void Initialize(SpellBase spell, BookSpellPage page)
    {
        this.spell = spell;
        parentPage = page;

        BuildSpellUI();

        completeButton.onClick.RemoveAllListeners();
        completeButton.onClick.AddListener(OnButtonClicked);

        UpdateState(false);
    }

    private void BuildSpellUI()
    {
        foreach (Transform child in letterContainer)
            Destroy(child.gameObject);

        activeSlots.Clear();

        string masked = spell.GetMaskedSpell();
        string full = spell.GetFullSpell();

        for (int i = 0; i < masked.Length; i++)
        {
            if (masked[i] == '_')
            {
                SpellLetterSlotUI slot =
                    Instantiate(slotPrefab, letterContainer);

                slot.Initialize(full[i], this);
                activeSlots.Add(slot);
            }
            else
            {
                GameObject textObj =
                    Instantiate(letterTextPrefab, letterContainer);

                textObj.GetComponent<TextMeshProUGUI>().text =
                    masked[i].ToString();
            }
        }
    }

    public void NotifySlotChanged()
    {
        foreach (var slot in activeSlots)
        {
            if (!slot.HasLetter())
            {
                completeButton.interactable = false;
                return;
            }
        }

        completeButton.interactable = true;
    }

    public void UpdateState(bool canComplete)
    {
        completeButton.interactable = canComplete;
    }

    private void OnButtonClicked()
    {
        parentPage.OnSpellButtonPressed(spell, this);
    }

    public bool AreAllSlotsFilled()
    {
        foreach (var slot in activeSlots)
        {
            if (!slot.HasLetter())
                return false;
        }

        return true;
    }

    public List<DraggableLetterUI> GetInsertedLetters()
    {
        List<DraggableLetterUI> letters = new List<DraggableLetterUI>();

        foreach (var slot in activeSlots)
        {
            var letter = slot.GetCurrentLetter();
            if (letter != null)
                letters.Add(letter);
        }

        return letters;
    }
}