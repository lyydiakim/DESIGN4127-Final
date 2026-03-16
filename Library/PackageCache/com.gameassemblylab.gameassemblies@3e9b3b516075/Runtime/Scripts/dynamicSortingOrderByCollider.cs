using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSortingOrderByCollider : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    [Header("Sorting Settings")]
    public bool invertOrder = false;
    public int sortingOrderOffset = 0;

    [Tooltip("If checked and a BoxCollider2D exists, use its lowest point instead of the sprite's")]
    public bool useBoxColliderBounds = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void FixedUpdate()
    {
        float lowestYPosition;

        // Determine whether to use box collider bounds or sprite bounds
        if (useBoxColliderBounds && boxCollider != null)
        {
            // Use the box collider's lowest point
            lowestYPosition = boxCollider.bounds.min.y;
        } else
        {
            // Use the sprite renderer's lowest point
            lowestYPosition = spriteRenderer.bounds.min.y;
        }

        // Determine the base sorting order from the y position
        int sortingOrder = -(int)(lowestYPosition * 100) + sortingOrderOffset;

        // Invert the sorting order if needed
        if (invertOrder)
        {
            sortingOrder = -sortingOrder;
        }

        // Apply the calculated sorting order to the sprite renderer
        spriteRenderer.sortingOrder = sortingOrder;
    }
}