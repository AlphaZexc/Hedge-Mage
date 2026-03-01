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

        letter.transform.SetParent(transform);
        RectTransform rect = letter.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
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