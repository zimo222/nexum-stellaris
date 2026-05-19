using UnityEngine;

public class DynamicSortingByY : MonoBehaviour
{
    public int sortingOrderBase = 0; // 基础排序值
    public int offsetMultiplier = 100; // 乘数，用于增加层级区分度

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSortingOrder();
    }

    void LateUpdate()
    {
        UpdateSortingOrder();
    }

    void UpdateSortingOrder()
    {
        if (spriteRenderer != null)
        {
            // 如果没有父物体，则回退到自身坐标
            Transform targetTransform = transform.parent != null ? transform.parent : transform;
            int newOrder = sortingOrderBase + Mathf.RoundToInt(-targetTransform.position.y * offsetMultiplier);
            spriteRenderer.sortingOrder = newOrder;
        }
    }
}