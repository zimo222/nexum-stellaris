using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // 新增

public class UIManager : Singleton<UIManager>
{
    public static UIManager Instance { get; private set; }
    [SerializeField] private List<BasePanel> allPanels;          // 动态面板列表
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Stack<BasePanel> panelStack = new Stack<BasePanel>();
    private bool isAnimating = false;

    protected override void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        // 单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        base.Awake();

        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 取消订阅
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成后自动刷新面板列表
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshPanelsForCurrentScene();
    }

    /// <summary>
    /// 刷新当前场景的所有面板：清空旧列表，重新扫描并注册
    /// </summary>
    public void RefreshPanelsForCurrentScene()
    {
        // 清空列表和堆栈
        allPanels.Clear();
        panelStack.Clear();

        // 查找场景中所有 BasePanel（包括未激活的）
        BasePanel[] panels = FindObjectsOfType<BasePanel>(true);
        foreach (var panel in panels)
        {
            RegisterPanel(panel, fromSceneLoad: true);
        }
    }

    /// <summary>
    /// 动态注册面板（由面板自身在Awake中调用，或场景刷新时调用）
    /// </summary>
    public void RegisterPanel(BasePanel panel, bool fromSceneLoad = false)
    {
        if (panel == null) return;

        // 避免重复注册
        if (!allPanels.Contains(panel))
        {
            allPanels.Add(panel);
        }

        // 设置初始显隐规则（注意：来自场景刷新的调用会覆盖之前可能的手动设置）
        if (panel.PanelName == "MainMenu")
        {
            // MainMenu 始终可见，并压入堆栈作为栈底
            panel.gameObject.SetActive(true);
            if (!panelStack.Contains(panel))
                panelStack.Push(panel);
        }
        else if (panel.InitializeVisible)
        {
            // 其他面板如果标记为初始可见，则显示但不压栈
            panel.gameObject.SetActive(true);
            // 不加入堆栈
        }
        else
        {
            // 默认隐藏
            panel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 注销面板（面板销毁时调用）
    /// </summary>
    public void UnregisterPanel(BasePanel panel)
    {
        if (panel != null && allPanels.Contains(panel))
            allPanels.Remove(panel);
    }

    /// <summary>
    /// 打开指定名称的面板
    /// </summary>
    public void OpenPanel(string panelName)
    {
        if (isAnimating)
        {
            Debug.LogWarning("UIManager is animating, cannot open panel now.");
            return;
        }

        var panel = allPanels.Find(p => p != null && p.PanelName == panelName);
        if (panel == null)
        {
            Debug.LogError($"Panel {panelName} not found!");
            return;
        }

        if (panelStack.Contains(panel))
        {
            Debug.LogWarning($"Panel {panelName} is already in the stack.");
            return;
        }

        BasePanel oldPanel = panelStack.Count > 0 ? panelStack.Peek() : null;

        panel.OnOpen();
        panelStack.Push(panel);

        StartCoroutine(AnimateSwitch(oldPanel, panel, true));
    }

    /// <summary>
    /// 关闭当前面板（返回上一个）
    /// </summary>
    public void CloseCurrentPanel()
    {
        if (isAnimating)
        {
            Debug.LogWarning("UIManager is animating, cannot close panel now.");
            return;
        }

        if (panelStack.Count == 0) return;

        BasePanel currentPanel = panelStack.Pop();
        BasePanel previousPanel = panelStack.Count > 0 ? panelStack.Peek() : null;

        StartCoroutine(AnimateSwitch(currentPanel, previousPanel, false));
    }

    /// <summary>
    /// 关闭所有面板
    /// </summary>
    public void CloseAll()
    {
        while (panelStack.Count > 0)
        {
            var panel = panelStack.Pop();
            panel.OnClose();
        }
    }

    private IEnumerator AnimateSwitch(BasePanel outPanel, BasePanel inPanel, bool isPush)
    {
        isAnimating = true;

        BasePanel referencePanel = outPanel ?? inPanel;
        if (referencePanel == null)
        {
            Debug.LogError("No panels to animate!");
            isAnimating = false;
            yield break;
        }

        RectTransform parentRect = referencePanel.GetComponent<RectTransform>().parent.GetComponent<RectTransform>();
        float canvasWidth = parentRect.rect.width;

        // 收集所有文本组件
        List<Text> outTexts = new List<Text>();
        List<TextMeshProUGUI> outTMPTexts = new List<TextMeshProUGUI>();
        List<Text> inTexts = new List<Text>();
        List<TextMeshProUGUI> inTMPTexts = new List<TextMeshProUGUI>();

        if (outPanel != null)
        {
            outTexts.AddRange(outPanel.GetComponentsInChildren<Text>(true));
            outTMPTexts.AddRange(outPanel.GetComponentsInChildren<TextMeshProUGUI>(true));
        }
        if (inPanel != null)
        {
            inPanel.gameObject.SetActive(true);
            inTexts.AddRange(inPanel.GetComponentsInChildren<Text>(true));
            inTMPTexts.AddRange(inPanel.GetComponentsInChildren<TextMeshProUGUI>(true));
            // 初始透明度设为0
            SetTextsAlpha(inTexts, 0f);
            SetTMPTextsAlpha(inTMPTexts, 0f);
        }

        Vector2 inStart = Vector2.zero;
        if (inPanel != null)
        {
            inStart = isPush ? new Vector2(canvasWidth, 0) : new Vector2(-canvasWidth, 0);
            inPanel.GetComponent<RectTransform>().anchoredPosition = inStart;
        }

        Vector2 outStart = outPanel != null ? outPanel.GetComponent<RectTransform>().anchoredPosition : Vector2.zero;
        Vector2 outEnd = Vector2.zero;
        if (outPanel != null)
        {
            outEnd = isPush ? new Vector2(-canvasWidth, 0) : new Vector2(canvasWidth, 0);
        }

        Vector2 inEnd = Vector2.zero;

        float elapsedTime = 0f;
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            float curveT = animationCurve.Evaluate(t);

            // 移动位置
            if (outPanel != null)
                outPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(outStart, outEnd, curveT);
            if (inPanel != null)
                inPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.Lerp(inStart, inEnd, curveT);

            // 透明度渐变
            if (outPanel != null)
            {
                float outAlpha = Mathf.Lerp(1f, 0f, curveT);
                SetTextsAlpha(outTexts, outAlpha);
                SetTMPTextsAlpha(outTMPTexts, outAlpha);
            }
            if (inPanel != null)
            {
                float inAlpha = Mathf.Lerp(0f, 1f, curveT);
                SetTextsAlpha(inTexts, inAlpha);
                SetTMPTextsAlpha(inTMPTexts, inAlpha);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 最终状态修正
        if (outPanel != null)
        {
            outPanel.GetComponent<RectTransform>().anchoredPosition = outEnd;
            // 恢复透明度为1（面板将被隐藏，但恢复默认值便于复用）
            SetTextsAlpha(outTexts, 1f);
            SetTMPTextsAlpha(outTMPTexts, 1f);

            if (!isPush) // 出栈时才调用 OnClose
                outPanel.OnClose();
            outPanel.gameObject.SetActive(false);
        }
        if (inPanel != null)
        {
            inPanel.GetComponent<RectTransform>().anchoredPosition = inEnd;
            SetTextsAlpha(inTexts, 1f);
            SetTMPTextsAlpha(inTMPTexts, 1f);
        }

        isAnimating = false;
    }

    private void SetTextsAlpha(List<Text> texts, float alpha)
    {
        foreach (var text in texts)
        {
            if (text != null)
            {
                Color c = text.color;
                c.a = alpha;
                text.color = c;
            }
        }
    }

    private void SetTMPTextsAlpha(List<TextMeshProUGUI> texts, float alpha)
    {
        foreach (var text in texts)
        {
            if (text != null)
            {
                Color c = text.color;
                c.a = alpha;
                text.color = c;
            }
        }
    }
};