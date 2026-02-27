using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableLetterUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image letterImage;
    public char Letter { get; private set; }

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Transform originalParent;
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(char c)
    {
        Letter = char.ToUpper(c);
        letterImage.sprite = LetterSpriteDatabase.Instance.GetWorldSprite(Letter);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // Detach from book page so it can move across pages
        transform.SetParent(canvas.transform);

        canvasGroup.blocksRaycasts = false; // allow drop detection
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // If still under canvas root, it wasn't dropped properly
        if (transform.parent == canvas.transform)
        {
            ReturnToOriginalPosition();
        }
    }

    public void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}