using UnityEngine;

/// <summary>
/// 挂在 UI Image 上，实现上下边界反弹的自动移动，支持边界停留
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UIMover : MonoBehaviour
{
    [Header("移动参数")]
    [Tooltip("每秒移动的距离（单位/秒）")]
    public float speed = 100f;

    [Tooltip("允许移动的最小 Y 坐标（相对父物体的锚点偏移）")]
    public float minY = 0f;

    [Tooltip("允许移动的最大 Y 坐标（相对父物体的锚点偏移）")]
    public float maxY = 400f;

    [Header("边界行为")]
    [Tooltip("到达边界后反向移动前停留的时间（秒），设为 0 则立即反向")]
    public float boundaryPauseTime = 0f;

    [Header("初始方向")]
    [Tooltip("是否初始向上移动")]
    public bool moveUpInitially = true;

    private RectTransform _rectTransform;
    private int _direction = 1;          // 1 = 向上，-1 = 向下
    private bool _isPausing = false;     // 是否处于边界停留状态
    private float _pauseTimer = 0f;      // 停留计时器

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform == null)
        {
            Debug.LogError("UIMover 需要 RectTransform 组件，请确保挂在 UI 元素上！");
            enabled = false;
            return;
        }

        // 设定初始方向
        _direction = moveUpInitially ? 1 : -1;

        // 确保起始位置不超出边界
        float clampedY = Mathf.Clamp(_rectTransform.anchoredPosition.y, minY, maxY);
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, clampedY);
    }

    private void Update()
    {
        if (_rectTransform == null) return;

        // 处理停留状态
        if (_isPausing)
        {
            _pauseTimer += Time.deltaTime;
            if (_pauseTimer >= boundaryPauseTime)
            {
                // 停留结束：反向移动，退出暂停
                _direction = -_direction;
                _isPausing = false;
            }
            return; // 停留期间不移动
        }

        // 正常移动状态
        float newY = _rectTransform.anchoredPosition.y + _direction * speed * Time.deltaTime;

        bool hitBoundary = false;

        // 边界检测（如果到达边界，先修正位置，然后根据停留时间决定是否进入暂停）
        if (newY > maxY)
        {
            newY = maxY;
            hitBoundary = true;
        }
        else if (newY < minY)
        {
            newY = minY;
            hitBoundary = true;
        }

        // 应用新位置
        _rectTransform.anchoredPosition = new Vector2(_rectTransform.anchoredPosition.x, newY);

        // 处理边界触发
        if (hitBoundary)
        {
            if (boundaryPauseTime > 0f)
            {
                // 需要停留：进入暂停状态，不清空方向（等待停留结束后反向）
                _isPausing = true;
                _pauseTimer = 0f;
            }
            else
            {
                // 无停留：立即反向
                _direction = -_direction;
            }
        }
    }

    // ---- 公共方法（可选，用于外部控制）----
    public void SetSpeed(float newSpeed) => speed = newSpeed;
    public void SetBoundaryPauseTime(float pauseTime) => boundaryPauseTime = pauseTime;

    /// <summary> 立即反向移动（如果处于停留状态，则会取消停留并立即反向）</summary>
    public void ReverseDirection()
    {
        if (_isPausing)
        {
            // 强制结束停留，直接反向并继续移动
            _isPausing = false;
        }
        _direction = -_direction;
    }

    /// <summary> 设置移动方向（true=向上，false=向下），如果处于停留状态则会先取消停留 </summary>
    public void SetDirection(bool upward)
    {
        if (_isPausing)
            _isPausing = false;
        _direction = upward ? 1 : -1;
    }

    /// <summary> 重置停留状态（手动解除暂停）</summary>
    public void CancelPause()
    {
        _isPausing = false;
    }
}