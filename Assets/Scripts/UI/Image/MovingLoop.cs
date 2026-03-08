using UnityEngine;

// 使物体在水平或垂直方向上匀速循环移动, 移动到结束边界后，立即重置到开始边界。
public class MovingLoop : MonoBehaviour
{
    // 移动轴方向
    public enum MoveAxis
    {
        Horizontal, // 水平（X轴）
        Vertical    // 垂直（Y轴）
    }

    [Header("移动设置")]
    public MoveAxis axis = MoveAxis.Horizontal;     // 移动轴
    [Tooltip("移动速度（正为右/上，负为左/下）")]
    public float speed = 1f;                        // 移动速度
    [Tooltip("开始边界（起始位置）")]
    public float startBoundary = 0f;                // 起始边界
    [Tooltip("结束边界（到达后重置）")]
    public float endBoundary = 10f;                 // 结束边界
    [Tooltip("启动时将物体置于开始边界")]
    public bool resetOnStart = true;                // 是否在启动时重置位置到开始边界

    private void Start()
    {
        if (resetOnStart)
        {
            SetPositionOnAxis(startBoundary);
        }
    }

    private void Update()
    {
        // 速度为0时不移动
        if (Mathf.Approximately(speed, 0f)) return;

        // 计算当前轴向位置 + 速度增量
        float currentPos = GetCurrentAxisPosition();
        float newPos = currentPos + speed * Time.deltaTime;

        // 根据速度方向检测是否越过结束边界
        if (speed > 0 && newPos >= endBoundary)
        {
            newPos = startBoundary;
        }
        else if (speed < 0 && newPos <= endBoundary)
        {
            newPos = startBoundary;
        }

        // 应用新位置
        SetPositionOnAxis(newPos);
    }

    // 获取当前轴向的世界坐标值
    private float GetCurrentAxisPosition()
    {
        return axis == MoveAxis.Horizontal ? transform.position.x : transform.position.y;
    }

    // 设置当前轴向的世界坐标值（保持其他轴不变）
    private void SetPositionOnAxis(float value)
    {
        Vector3 pos = transform.position;
        if (axis == MoveAxis.Horizontal)
            pos.x = value;
        else
            pos.y = value;
        transform.position = pos;
    }
}