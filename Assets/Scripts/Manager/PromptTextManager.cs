using UnityEngine;
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
    [Tooltip("TextMeshPro 文本组件，如果不指定则自动在子物体中查找")]
    [SerializeField] private GameObject Canvas;
    [SerializeField] private TMP_Text textComponent;

    [Header("设置")]
    [Tooltip("默认显示时间（秒），当调用时不传时间或传值<=0时使用")]
    [SerializeField] private float defaultDisplayTime = 2f;

    private GameObject textGameObject;   // 文本物体（用于激活/禁用）
    private Coroutine currentCoroutine;  // 当前正在运行的协程

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

        // 确保初始状态为禁用（符合默认子对象禁用）
        if (textGameObject.activeSelf)
        {
            Canvas.SetActive(false);
            textGameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // 当管理器自身被禁用时，停止当前协程，防止残留
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
    public void ShowMessage(string message, float duration = -1f)
    {
        if (textComponent == null)
        {
            Debug.LogError("PromptTextManager: 无法显示消息，TMP_Text 组件未设置或未找到。");
            return;
        }

        // 确定显示时长
        float displayTime = (duration > 0) ? duration : defaultDisplayTime;

        // 如果时长极小或为零，直接隐藏（避免瞬间显示）
        if (displayTime <= 0.01f)
        {
            HideImmediately();
            return;
        }

        // 停止当前正在运行的协程，以重置计时
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }

        // 更新文本内容
        textComponent.text = message;

        // 激活文本物体（如果尚未激活）
        if (!textGameObject.activeSelf)
        {
            Canvas.SetActive(true);
            textGameObject.SetActive(true);
        }

        // 启动新的隐藏协程
        currentCoroutine = StartCoroutine(HideAfterDelay(displayTime));
    }

    /// <summary>
    /// 协程：等待指定时间后隐藏文本物体
    /// </summary>
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 隐藏文本物体
        if (textGameObject != null && textGameObject.activeSelf)
        {
            Canvas.SetActive(false);
            textGameObject.SetActive(false);
        }

        currentCoroutine = null;
    }

    /// <summary>
    /// 立即隐藏当前显示的文本，并停止计时
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
            Canvas.SetActive(false);
            textGameObject.SetActive(false);
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