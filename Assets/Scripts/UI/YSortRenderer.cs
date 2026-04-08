using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortRenderer : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    [Tooltip("Offset applied to Y before sorting. Positive moves the sort point up.")]
    public float yOffset = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // Use world-space Y of this transform + offset
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-(transform.position.y + yOffset) * 100);
    }
}