using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleAIChat : MonoBehaviour
{
    public UnifiedAIManager aiManager;
    public TMP_InputField inputField;
    public Button sendButton;
    public Transform messageContainer;
    public GameObject messageTextPrefab;
    public GameObject buttonPrefab;

    void Start()
    {
        sendButton.onClick.AddListener(SendMessage);
        inputField.onSubmit.AddListener(delegate { SendMessage(); });
    }

    void SendMessage()
    {
        string userMessage = inputField.text;
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        AddMessage($"我: {userMessage}");
        inputField.text = "";

        aiManager.SendMessageToAI(userMessage, OnAIResponse);
    }

    void OnAIResponse(string aiReply)
    {
        string trimmed = aiReply.Trim();

        // 尝试找到按钮 JSON 的起始位置
        int jsonStart = -1;
        int jsonEnd = -1;
        const string buttonMarker = "\"type\":\"button\"";
        int markerIndex = trimmed.IndexOf(buttonMarker);

        if (markerIndex >= 0)
        {
            // 往前找到第一个 '{'
            int braceStart = markerIndex;
            while (braceStart >= 0 && trimmed[braceStart] != '{')
                braceStart--;
            if (braceStart >= 0)
            {
                // 往后找到匹配的 '}'
                int braceCount = 0;
                for (int i = braceStart; i < trimmed.Length; i++)
                {
                    if (trimmed[i] == '{') braceCount++;
                    else if (trimmed[i] == '}') braceCount--;
                    if (braceCount == 0)
                    {
                        jsonEnd = i;
                        break;
                    }
                }
                if (jsonEnd > braceStart)
                {
                    string jsonStr = trimmed.Substring(braceStart, jsonEnd - braceStart + 1);
                    try
                    {
                        ButtonData data = JsonUtility.FromJson<ButtonData>(jsonStr);
                        if (data != null && !string.IsNullOrEmpty(data.label))
                        {
                            // 提取 JSON 前面的文本
                            string beforeJson = trimmed.Substring(0, braceStart).Trim();
                            if (!string.IsNullOrEmpty(beforeJson))
                            {
                                AddMessage($"纯白: {beforeJson}", false);
                            }
                            CreateButton(data);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"解析按钮失败: {e.Message}\nJSON 部分: {jsonStr}");
                    }
                }
            }
        }

        // 未找到有效按钮 JSON，当作普通文本
        AddMessage($"纯白: {aiReply}", false);
    }

    void AddMessage(string text, bool isPlayer = true) // 根据你的实际情况调整
    {
        GameObject prefab = isPlayer ? messageTextPrefab : messageTextPrefab;
        GameObject msgObj = Instantiate(prefab, messageContainer);
        msgObj.GetComponentInChildren<TMP_Text>().text = text;

        // 启动协程，等待一帧后滚动到底部
        StartCoroutine(ScrollToBottom());
    }

    System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        ScrollRect scrollRect = messageContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // 0 = 底部
        }
    }

    void CreateButton(ButtonData data)
    {
        GameObject btnObj = Instantiate(buttonPrefab, messageContainer);
        Button btn = btnObj.GetComponent<Button>();
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        btnText.text = data.label;
        btn.onClick.AddListener(() => ExecuteAction(data.action, data.@params));

        // 强制立即重建 Content 的布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);
        // 然后滚动到底部（仍然需要一点延迟，但可能更可靠）
        ScrollToBottom(0.05f); // 短延迟
    }

    // 修改原有的 ScrollToBottom 支持参数
    void ScrollToBottom(float delay = 0.1f)
    {
        StartCoroutine(ScrollToBottomCoroutine(delay));
    }

    IEnumerator ScrollToBottomCoroutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ScrollRect scrollRect = messageContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void ExecuteAction(string action, ActionParams param)
    {
        switch (action)
        {
            case "teleport":
                if (param == null || string.IsNullOrEmpty(param.location))
                {
                    AddMessage("纯白: 你想传送到哪里呢？", false);
                    return;
                }

                string locationName = param.location;
                if (TryGetSceneInfo(locationName, out string sceneName, out float x, out float y))
                {
                    AddMessage($"系统: 正在传送到 {locationName}...", false);
                    // 调用你的场景管理器
                    SceneDataManager.Instance.LoadScene(sceneName, (int)x, (int)y);
                }
                else
                {
                    AddMessage($"纯白: 我不认识「{locationName}」。我只知道家、回廊、原野、工坊、大厅、花地这些地方……", false);
                }
                break;
            case "open_workshop":
                Debug.Log("打开工坊");
                AddMessage("系统: 工坊已打开。");
                break;
            case "comfort":
                AddMessage("纯白轻轻握住你的手，银白色的发梢拂过你的掌心……");
                break;
            default:
                Debug.LogWarning($"未知动作: {action}");
                AddMessage($"纯白: 我好像不知道如何执行「{action}」……");
                break;
        }
    }

    [Serializable]
    public class ButtonData
    {
        public string type;
        public string label;
        public string action;
        public ActionParams @params;   // 使用 @ 转义关键字
    }

    [Serializable]
    public class ActionParams
    {
        public string location;
    }

    // 场景映射：地名 -> (场景名, 出生坐标X, 出生坐标Y)
    private readonly Dictionary<string, (string sceneName, float x, float y)> sceneMap =
        new Dictionary<string, (string, float, float)>
    {
    // 暖光之巢
    { "家", ("1_TheNestOfWarmLight", 0f, 0f) },
    { "村", ("1_TheNestOfWarmLight", 0f, 0f) },
    { "暖光之巢", ("1_TheNestOfWarmLight", 0f, 0f) },
    { "nest", ("1_TheNestOfWarmLight", 0f, 0f) },
    
    // 纯白回廊
    { "纯白回廊", ("2_TheArgentCorridor", 0f, 0f) },
    { "回廊", ("2_TheArgentCorridor", 0f, 0f) },
    { "corridor", ("2_TheArgentCorridor", 0f, 0f) },
    
    // 初萌原野
    { "原野", ("3_TheVerdantMeadow", 0f, 100f) },
    { "草原", ("3_TheVerdantMeadow", 0f, 100f) },
    { "大树下", ("3_TheVerdantMeadow", 0f, 100f) },
    { "meadow", ("3_TheVerdantMeadow", 0f, 100f) },
    { "初萌原野", ("3_TheVerdantMeadow", 0f, 100f) },
    
    // 痴迷工坊
    { "工坊", ("4_TheWorkshopOfPassion", 0f, 0f) },
    { "痴迷工坊", ("4_TheWorkshopOfPassion", 0f, 0f) },
    { "workshop", ("4_TheWorkshopOfPassion", 0f, 0f) },
    
    // 万物共鸣大厅
    { "大厅", ("5_TheHallOfUniversalConcord", 0f, 0f) },
    { "共鸣大厅", ("5_TheHallOfUniversalConcord", 0f, 0f) },
    { "决战之地", ("5_TheHallOfUniversalConcord", 0f, 0f) },
    { "hall", ("5_TheHallOfUniversalConcord", 0f, 0f) },
    
    // 大结局花地
    { "花地", ("6_TheStellarWish", 0f, 0f) },
    { "星愿花海", ("6_TheStellarWish", 0f, 0f) },
    { "stellar", ("6_TheStellarWish", 0f, 0f) },
    };

    // 如果地名未匹配，返回 false
    private bool TryGetSceneInfo(string locationName, out string sceneName, out float x, out float y)
    {
        if (sceneMap.TryGetValue(locationName, out var info))
        {
            sceneName = info.sceneName;
            x = info.x;
            y = info.y;
            return true;
        }
        // 尝试忽略大小写和空格
        foreach (var kvp in sceneMap)
        {
            if (string.Compare(locationName, kvp.Key, StringComparison.OrdinalIgnoreCase) == 0)
            {
                sceneName = kvp.Value.sceneName;
                x = kvp.Value.x;
                y = kvp.Value.y;
                return true;
            }
        }
        sceneName = null;
        x = 0;
        y = 0;
        return false;
    }
}

