using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableZoomableMap : MonoBehaviour, IDragHandler, IPointerDownHandler, IScrollHandler
{
    [Header("缩放设置")]
    [SerializeField] private float zoomSpeed = 0.1f;      // 滚轮缩放速度
    [SerializeField] private float minScale = 0.5f;       // 最小缩放比例
    [SerializeField] private float maxScale = 3f;         // 最大缩放比例

    private RectTransform rectTransform;
    private Vector2 originalSize;
    private Vector2 dragStartPos;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalSize = rectTransform.sizeDelta;
    }

    // 鼠标按下：记录初始位置（用于拖拽）
    public void OnPointerDown(PointerEventData eventData)
    {
        dragStartPos = rectTransform.anchoredPosition - eventData.position; // 记录偏移
    }

    // 鼠标拖拽移动
    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition = dragStartPos + eventData.position;
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
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, mouseWorldPos, eventData.pressEventCamera, out localPoint);

        // 缩放前世界位置
        Vector3 oldWorldPos = rectTransform.TransformPoint(localPoint);

        // 应用缩放
        rectTransform.localScale = newScale;

        // 缩放后相同局部点的新世界位置
        Vector3 newWorldPos = rectTransform.TransformPoint(localPoint);

        // 补偿位移，使鼠标下世界点不变
        rectTransform.position += oldWorldPos - newWorldPos;
    }
}