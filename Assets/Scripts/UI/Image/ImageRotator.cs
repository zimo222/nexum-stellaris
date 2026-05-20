using UnityEngine;

public class ImageRotator : MonoBehaviour
{
    [Header("旋转设置")]
    [Tooltip("旋转速度（度/秒）")]
    public float rotationSpeed = 90f;

    [Tooltip("是否顺时针旋转")]
    public bool clockwise = true;

    [Tooltip("是否在开始时自动旋转")]
    public bool autoStart = true;

    private bool isRotating;

    void Start()
    {
        isRotating = autoStart;
    }

    void Update()
    {
        if (!isRotating) return;

        float direction = clockwise ? 1f : -1f;
        float angle = rotationSpeed * direction * Time.deltaTime;
        transform.Rotate(0f, 0f, angle);
    }

    /// <summary>开始旋转</summary>
    public void StartRotation() => isRotating = true;

    /// <summary>停止旋转</summary>
    public void StopRotation() => isRotating = false;

    /// <summary>设置旋转速度（正值）</summary>
    public void SetRotationSpeed(float speed) => rotationSpeed = Mathf.Abs(speed);

    /// <summary>设置旋转方向</summary>
    public void SetClockwise(bool clockwiseDirection) => clockwise = clockwiseDirection;
}