using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LetterDropSlotUI : MonoBehaviour, IDropHandler
{
    public char requiredLetter;
    public Image slotImage;

    protected DraggableLetterUI currentLetter;

    private void Awake()
    {
        requiredLetter = char.ToUpper(requiredLetter);
    }

    public void OnDrop(PointerEventData eventData)
    {
        DraggableLetterUI draggedLetter =
            eventData.pointerDrag?.GetComponent<DraggableLetterUI>();

        if (draggedLetter == null)
            return;

        // Reject wrong letter
        if (draggedLetter.Letter != requiredLetter)
        {
            draggedLetter.ReturnToOriginalPosition();
            return;
        }

        // If slot already occupied, reject
        if (currentLetter != null)
        {
            draggedLetter.ReturnToOriginalPosition();
            return;
        }

        AcceptLetter(draggedLetter);
    }

    protected virtual void AcceptLetter(DraggableLetterUI letter)
    {
        currentLetter = letter;

        RectTransform rect = letter.GetComponent<RectTransform>();

        letter.transform.SetParent(transform, false); // VERY IMPORTANT

        // Force proper anchoring
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = ((RectTransform)transform).rect.size;
        rect.localScale = Vector3.one; // prevent weird scaling issues

        gameObject.GetComponent<Image>().enabled = false; // hide slot background
    }

    public bool HasLetter()
    {
        return currentLetter != null;
    }

    public DraggableLetterUI GetCurrentLetter()
    {
        return currentLetter;
    }

    public void ClearSlot()
    {
        currentLetter = null;
    }
}