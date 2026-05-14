using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class UIRingRotator : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("旋转灵敏度（1=原速，0.5=半速）")]
    public float sensitivity = 0.5f;

    [Tooltip("是否反转旋转方向")]
    public bool reverseDirection = true;

    [Tooltip("吸附等分数（例如6表示将圆分成6份，每份60度）")]
    public int snapDivisions = 6;

    [Tooltip("吸附时是否使用平滑动画")]
    public bool smoothSnap = true;

    [Tooltip("平滑吸附时间（秒）")]
    public float snapDuration = 0.2f;

    [Tooltip("圆环外半径（相对于圆盘宽度的一半）")]
    public float outerRadius = 1f;

    [Tooltip("圆环内半径比例（0~1），0.7表示从外半径的70%处开始算圆环")]
    [Range(0f, 0.9f)]
    public float innerRadiusRatio = 0.7f;

    [Tooltip("需要反向旋转的子对象（保持它们的世界方向不变）")]
    public Transform[] invertedChildren;   // 在Inspector中拖入子对象

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

            deltaAngle *= sensitivity;

            if (reverseDirection)
                deltaAngle = -deltaAngle;

            // 旋转圆盘
            rectTransform.Rotate(0, 0, deltaAngle);

            // 反向旋转子对象，保持它们的世界方向不变
            RotateChildren(-deltaAngle);

            lastMousePos = currentLocalPoint;
            lastAngle = currentAngle;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

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
        float delta = Mathf.DeltaAngle(currentZ, targetZ);

        rectTransform.eulerAngles = new Vector3(0, 0, targetZ);
        RotateChildren(-delta);  // 子对象反向旋转相同的角度
    }

    // 平滑吸附（协程）
    private IEnumerator SmoothSnapToNearestAngle()
    {
        float step = 360f / snapDivisions;
        float startZ = rectTransform.eulerAngles.z;
        float targetZ = Mathf.Round(startZ / step) * step;
        float delta = Mathf.DeltaAngle(startZ, targetZ);
        targetZ = startZ + delta;

        float elapsed = 0f;
        float lastZ = startZ;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapDuration;
            float newZ = Mathf.Lerp(startZ, targetZ, t);
            float deltaThisFrame = Mathf.DeltaAngle(lastZ, newZ);
            rectTransform.eulerAngles = new Vector3(0, 0, newZ);
            RotateChildren(-deltaThisFrame);  // 每帧同步反向旋转子对象
            lastZ = newZ;
            yield return null;
        }
        rectTransform.eulerAngles = new Vector3(0, 0, targetZ);
        RotateChildren(-Mathf.DeltaAngle(lastZ, targetZ)); // 确保最后一帧精准
        snapCoroutine = null;
    }

    // 反向旋转子对象
    private void RotateChildren(float angle)
    {
        if (invertedChildren == null) return;
        foreach (Transform child in invertedChildren)
        {
            if (child != null)
                child.Rotate(0, 0, angle, Space.Self);
        }
    }
}