using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果使用 TextMeshPro，需要此命名空间

/// <summary>
/// 让文本的缩放值在指定范围内来回循环变动（类似心跳或呼吸效果）
/// 支持 UGUI Text 和 TextMeshPro
/// </summary>
public class TextScalePingPong : MonoBehaviour
{
    [Header("缩放范围")]
    [Tooltip("最小缩放值（例如 0.8）")]
    public float minScale = 0.8f;

    [Tooltip("最大缩放值（例如 1.2）")]
    public float maxScale = 1.2f;

    [Header("动画速度")]
    [Tooltip("每秒完成多少个完整的缩放循环（值越大，变化越快）")]
    public float cyclesPerSecond = 1f;

    [Header("起始位置")]
    [Tooltip("从哪个缩放值开始（0=min, 1=max, 0.5=中间）")]
    [Range(0f, 1f)]
    public float startOffset = 0f; // 0~1 的偏移

    [Header("曲线类型")]
    public PingPongCurveType curveType = PingPongCurveType.SineWave;

    public enum PingPongCurveType
    {
        SineWave,   // 平滑的正弦波
        Linear      // 线性的来回（三角形波）
    }

    // 缓存原始组件
    private RectTransform rectTransform;

    // 当前动画时间（秒）
    private float time;

    void Awake()
    {
        // 获取 RectTransform（UI 元素都挂载 RectTransform）
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("TextScalePingPong 需要挂载在带有 RectTransform 的物体上（例如 Text 或 TextMeshPro）");
            enabled = false;
            return;
        }

        // 初始化时间，使缩放从指定的偏移开始
        float period = 1f / cyclesPerSecond;
        time = period * startOffset;
    }

    void Update()
    {
        if (rectTransform == null) return;

        // 时间累加（deltaTime 确保速度与帧率无关）
        time += Time.deltaTime;

        // 计算当前周期的进度 t（范围 0~1，来回振荡）
        float t = GetPingPongValue(time);

        // 将 t 映射到 [minScale, maxScale]
        float currentScale = Mathf.Lerp(minScale, maxScale, t);

        // 应用缩放（保持原来的 x 和 y 同步变化，z 不变）
        rectTransform.localScale = new Vector3(currentScale, currentScale, rectTransform.localScale.z);
    }

    /// <summary>
    /// 根据时间和曲线类型，返回 0~1 之间的来回振荡值
    /// </summary>
    private float GetPingPongValue(float time)
    {
        float period = 1f / cyclesPerSecond;
        float normalized = time / period; // 经过了多少个完整周期

        if (curveType == PingPongCurveType.SineWave)
        {
            // 正弦波：范围 [0,1]，一个完整周期从 0->1->0
            // sin(2π * normalized) 范围 -1..1，映射到 0..1
            float sinVal = Mathf.Sin(2f * Mathf.PI * normalized);
            return (sinVal + 1f) / 2f;
        }
        else // Linear
        {
            // 线性三角波：使用 Mathf.PingPong，范围 [0,1]
            return Mathf.PingPong(normalized, 1f);
        }
    }

    /// <summary>
    /// 可选：在编辑器模式下测试时，如果参数改变，立即重新计算时间偏移
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && rectTransform != null)
        {
            float period = 1f / cyclesPerSecond;
            time = period * startOffset;
        }
    }
}