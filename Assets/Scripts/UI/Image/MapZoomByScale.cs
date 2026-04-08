using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableZoomableMap : MonoBehaviour, IDragHandler, IPointerDownHandler, IScrollHandler
{
    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 0.1f;      // 滚轮缩放速度
    [SerializeField] private float minScale = 0.5f;       // 最小缩放比例
    [SerializeField] private float maxScale = 3f;         // 最大缩放比例

    [Header("边界限制（基于 anchoredPosition）")]
    [SerializeField] private float leftBound = -1000f;     // 左边界（最小 X）
    [SerializeField] private float rightBound = 1000f;     // 右边界（最大 X）
    [SerializeField] private float bottomBound = -1000f;   // 下边界（最小 Y）
    [SerializeField] private float topBound = 1000f;       // 上边界（最大 Y）

    private RectTransform rectTransform;
    private Vector2 originalSize;
    private Vector2 dragStartPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.sizeDelta;
    }

    void Start()
    {
        // 确保初始位置在边界内
        ClampPosition();
    }

    // 鼠标按下：记录初始位置（用于拖拽）
    public void OnPointerDown(PointerEventData eventData)
    {
        dragStartPos = rectTransform.anchoredPosition - eventData.position;
    }

    // 鼠标拖拽移动
    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = dragStartPos + eventData.position;
        ClampPosition(); // 拖拽后限制边界
    }

    // 滚轮缩放
    public void OnScroll(PointerEventData eventData)
    {
        // 计算新的缩放比例（基于localScale）
        float scaleFactor = 1 + eventData.scrollDelta.y * zoomSpeed;
        Vector3 newScale = rectTransform.localScale * scaleFactor;
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = 1;

        // 以鼠标为中心缩放
        Vector2 mouseWorldPos = eventData.position; // 鼠标屏幕位置
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, mouseWorldPos, eventData.pressEventCamera, out Vector2 localPoint);

        // 缩放前世界位置
        Vector3 oldWorldPos = rectTransform.TransformPoint(localPoint);

        // 应用缩放
        rectTransform.localScale = newScale;

        // 缩放后相同局部点的新世界位置
        Vector3 newWorldPos = rectTransform.TransformPoint(localPoint);

        // 补偿位移，使鼠标下世界点不变
        rectTransform.position += oldWorldPos - newWorldPos;

        // 缩放后限制边界
        ClampPosition();
    }

    /// <summary>
    /// 将地图的 anchoredPosition 限制在设定的边界范围内
    /// </summary>
    private void ClampPosition()
    {
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, leftBound, rightBound);
        pos.y = Mathf.Clamp(pos.y, bottomBound, topBound);
        rectTransform.anchoredPosition = pos;
    }
}