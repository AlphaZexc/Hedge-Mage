using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortRenderer : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Tooltip("Offset to apply to Y-position for sorting (e.g., if pivot isn't at the feet).")]
    public float yOffset = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        float yPosition = transform.position.y + yOffset;
        spriteRenderer.sortingOrder = -(int)(yPosition * 100);
    }
}
