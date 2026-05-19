using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 提示文本管理器
/// 挂载到空物体上，要求其子物体包含一个 TMP_Text 组件，且子物体默认为禁用状态。
/// 调用 ShowMessage 方法可显示文本，并在一段时间后自动隐藏。
/// </summary>
public class PromptTextManager : MonoBehaviour
{
    public static PromptTextManager Instance { get; private set; }

    [Header("组件引用")]
    [Tooltip("父级 Canvas 物体（用于整体激活/禁用）")]
    [SerializeField] private GameObject Canvas;

    [Tooltip("背景图片物体（可选，需要其上挂载 Image 组件）")]
    [SerializeField] private GameObject Image;

    [Tooltip("TextMeshPro 文本组件，如果不指定则自动在子物体中查找")]
    [SerializeField] private TMP_Text textComponent;

    [Header("设置")]
    [Tooltip("默认显示时间（秒），当调用时不传时间或传值<=0时使用")]
    [SerializeField] private float defaultDisplayTime = 2f;

    private GameObject textGameObject;   // 文本物体（用于激活/禁用）
    private Coroutine currentCoroutine;  // 当前正在运行的协程
    private Image backgroundImage;       // 背景图片组件（从 Image 物体上获取）

    // 用于记录原始位置（局部坐标，避免受父物体移动影响）
    private Vector3 originalTextLocalPos;
    private Vector3 originalBgLocalPos;
    private bool hasOriginalPos = false; // 是否已记录原始位置

    private void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 如果没有手动指定 textComponent，尝试在子物体中查找（包括非激活状态的子物体）
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TMP_Text>(true);
            if (textComponent == null)
            {
                Debug.LogError("PromptTextManager: 在物体及其子物体中未找到 TMP_Text 组件！请确保存在一个带有 TMP_Text 组件的子物体。");
                enabled = false;
                return;
            }
        }

        textGameObject = textComponent.gameObject;

        // 获取背景图片组件（如果 Image 物体存在且挂载了 Image 组件）
        if (Image != null)
        {
            backgroundImage = Image.GetComponent<Image>();
            if (backgroundImage == null)
            {
                Debug.LogWarning("PromptTextManager: Image 物体存在，但未找到 Image 组件，背景颜色将无法生效。");
            }
        }
        else
        {
            backgroundImage = null;
        }

        // 记录原始局部位置（确保在初始状态下记录，即使物体是禁用的）
        originalTextLocalPos = textComponent.transform.localPosition;
        if (backgroundImage != null)
        {
            originalBgLocalPos = backgroundImage.transform.localPosition;
        }
        hasOriginalPos = true;

        // 确保初始状态为禁用
        if (textGameObject.activeSelf)
        {
            Canvas.SetActive(false);
            textGameObject.SetActive(false);
            if (Image != null) Image.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
    }

    /// <summary>
    /// 显示一段文本，并在指定时间后自动隐藏
    /// </summary>
    /// <param name="message">要显示的文本内容</param>
    /// <param name="duration">显示时长（秒），可选，不传或 <=0 则使用默认时长</param>
    /// <param name="fontColor">字体颜色，默认纯白色（Color.white）</param>
    /// <param name="backgroundColor">背景颜色，默认纯黑色（Color.black）</param>
    /// <param name="y">纵向偏移量（像素或世界单位，取决于Canvas模式），正数向上，负数向下</param>
    public void ShowMessage(string message, float duration = -1f, Color? fontColor = null, Color? backgroundColor = null, int y = 0)
    {
        if (textComponent == null)
        {
            Debug.LogError("PromptTextManager: 无法显示消息，TMP_Text 组件未设置或未找到。");
            return;
        }

        // 1. 恢复原始位置（清除上次可能残留的偏移）
        RestoreOriginalPositions();

        // 2. 应用字体颜色
        textComponent.color = fontColor ?? Color.white;

        // 3. 应用背景颜色
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor ?? Color.black;
        }
        else if (backgroundColor.HasValue)
        {
            Debug.LogWarning("PromptTextManager: 未找到背景图片组件，无法设置背景颜色。");
        }

        // 4. 应用纵向偏移（基于原始位置）
        if (y != 0)
        {
            Vector3 newTextPos = originalTextLocalPos + new Vector3(0, y, 0);
            textComponent.transform.localPosition = newTextPos;

            if (backgroundImage != null)
            {
                Vector3 newBgPos = originalBgLocalPos + new Vector3(0, y, 0);
                backgroundImage.transform.localPosition = newBgPos;
            }
        }

        // 5. 确定显示时长
        float displayTime = (duration > 0) ? duration : defaultDisplayTime;
        if (displayTime <= 0.01f)
        {
            HideImmediately();
            return;
        }

        // 6. 停止当前协程
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        // 7. 更新文本内容
        textComponent.text = message;

        // 8. 激活所有相关物体
        if (!textGameObject.activeSelf)
        {
            if (Canvas != null) Canvas.SetActive(true);
            if (Image != null) Image.SetActive(true);
            textGameObject.SetActive(true);
        }

        // 9. 启动隐藏协程
        currentCoroutine = StartCoroutine(HideAfterDelay(displayTime));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideImmediately();
    }

    /// <summary>
    /// 立即隐藏当前显示的文本，并停止计时，同时恢复原始位置
    /// </summary>
    public void HideImmediately()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        if (textGameObject != null && textGameObject.activeSelf)
        {
            if (Canvas != null) Canvas.SetActive(false);
            if (Image != null) Image.SetActive(false);
            textGameObject.SetActive(false);
        }

        // 恢复原始位置
        RestoreOriginalPositions();
    }

    /// <summary>
    /// 将文本和背景的位置恢复为原始记录值
    /// </summary>
    private void RestoreOriginalPositions()
    {
        if (!hasOriginalPos) return;

        textComponent.transform.localPosition = originalTextLocalPos;
        if (backgroundImage != null)
        {
            backgroundImage.transform.localPosition = originalBgLocalPos;
        }
    }

    /// <summary>
    /// 动态修改默认显示时间
    /// </summary>
    /// <param name="newDefaultTime">新的默认显示时长（秒）</param>
    public void SetDefaultDisplayTime(float newDefaultTime)
    {
        if (newDefaultTime > 0)
        {
            defaultDisplayTime = newDefaultTime;
        }
    }
}