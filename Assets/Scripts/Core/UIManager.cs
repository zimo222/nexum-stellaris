using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 若未使用 TextMeshPro 可删除此行及相关代码

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private BasePanel[] allPanels;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Stack<BasePanel> panelStack = new Stack<BasePanel>();
    private bool isAnimating = false;

    protected override void Awake()
    {
        base.Awake();
        foreach (var panel in allPanels)
        {
            if (panel.PanelName != "MainMenu")
            {
                panel.gameObject.SetActive(false);
            }
            else
            {
                panel.OnOpen();
                panel.gameObject.SetActive(true);
                panelStack.Push(panel);
            }
        }
    }

    public void OpenPanel(string panelName)
    {
        if (isAnimating)
        {
            Debug.LogWarning("UIManager is animating, cannot open panel now.");
            return;
        }

        var panel = System.Array.Find(allPanels, p => p.PanelName == panelName);
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
}