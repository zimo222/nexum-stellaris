using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class UIRingRotator : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Tooltip("旋转灵敏度（1=原速，0.5=半速）")]
    public float sensitivity = 0.5f;

    [Tooltip("是否反转旋转方向")]
    public bool reverseDirection = true;   // 设为 true 可解决“划拉反过来”的问题

    [Tooltip("吸附等分数（例如6表示将圆分成6份，每份60度）")]
    public int snapDivisions = 6;          // 0 或 1 表示不吸附

    [Tooltip("吸附时是否使用平滑动画")]
    public bool smoothSnap = true;

    [Tooltip("平滑吸附时间（秒）")]
    public float snapDuration = 0.2f;

    [Tooltip("圆环外半径（相对于圆盘宽度的一半）")]
    public float outerRadius = 1f;

    [Tooltip("圆环内半径比例（0~1），0.7表示从外半径的70%处开始算圆环")]
    [Range(0f, 0.9f)]
    public float innerRadiusRatio = 0.7f;

    private RectTransform rectTransform;
    private float innerRadius;
    private bool isDragging = false;
    private Vector2 lastMousePos;
    private float lastAngle;
    private Coroutine snapCoroutine = null;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;
        float maxSize = Mathf.Max(width, height);
        outerRadius = maxSize * 0.5f;
        innerRadius = outerRadius * innerRadiusRatio;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 如果正在吸附动画，先停止并直接完成当前角度（避免冲突）
        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            float dist = localPoint.magnitude;
            if (dist >= innerRadius && dist <= outerRadius)
            {
                isDragging = true;
                lastMousePos = localPoint;
                lastAngle = GetAngle(localPoint);
            }
            else
            {
                isDragging = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 currentLocalPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out currentLocalPoint))
        {
            float currentAngle = GetAngle(currentLocalPoint);
            float deltaAngle = Mathf.DeltaAngle(lastAngle, currentAngle);

            // 应用灵敏度
            deltaAngle *= sensitivity;

            // 方向反转
            if (reverseDirection)
                deltaAngle = -deltaAngle;

            // 旋转 UI 对象
            rectTransform.Rotate(0, 0, deltaAngle);

            lastMousePos = currentLocalPoint;
            lastAngle = currentAngle;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        // 吸附功能
        if (snapDivisions > 1)
        {
            if (smoothSnap)
            {
                if (snapCoroutine != null)
                    StopCoroutine(snapCoroutine);
                snapCoroutine = StartCoroutine(SmoothSnapToNearestAngle());
            }
            else
            {
                SnapToNearestAngle();
            }
        }
    }

    private float GetAngle(Vector2 localPoint)
    {
        return Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
    }

    // 瞬间吸附
    private void SnapToNearestAngle()
    {
        float step = 360f / snapDivisions;
        float currentZ = rectTransform.eulerAngles.z;
        float targetZ = Mathf.Round(currentZ / step) * step;
        rectTransform.eulerAngles = new Vector3(0, 0, targetZ);
    }

    // 平滑吸附（协程）
    private IEnumerator SmoothSnapToNearestAngle()
    {
        float step = 360f / snapDivisions;
        float startZ = rectTransform.eulerAngles.z;
        float targetZ = Mathf.Round(startZ / step) * step;

        // 处理跨越0度的情况（例如从350度转到10度，应该走20度而不是-340度）
        float delta = Mathf.DeltaAngle(startZ, targetZ);
        targetZ = startZ + delta;

        float elapsed = 0f;
        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapDuration;
            float newZ = Mathf.Lerp(startZ, targetZ, t);
            rectTransform.eulerAngles = new Vector3(0, 0, newZ);
            yield return null;
        }
        rectTransform.eulerAngles = new Vector3(0, 0, targetZ);
        snapCoroutine = null;
    }
}