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
    public Transform messageContainer;          // Content
    public GameObject playerMessagePrefab;      // 玩家消息预制体
    public GameObject aiMessagePrefab;          // AI消息预制体
    public GameObject buttonPrefab;             // 按钮预制体

    void Start()
    {
        sendButton.onClick.AddListener(SendMessage);
        inputField.onSubmit.AddListener(delegate { SendMessage(); });
    }

    void SendMessage()
    {
        string userMessage = inputField.text;
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        AddMessage($"我: {userMessage}", true);
        inputField.text = "";

        aiManager.SendMessageToAI(userMessage, OnAIResponse);
    }

    void OnAIResponse(string aiReply)
    {
        string trimmed = aiReply.Trim();

        // 查找按钮 JSON
        int jsonStart = -1;
        int jsonEnd = -1;
        const string buttonMarker = "\"type\":\"button\"";
        int markerIndex = trimmed.IndexOf(buttonMarker);

        if (markerIndex >= 0)
        {
            int braceStart = markerIndex;
            while (braceStart >= 0 && trimmed[braceStart] != '{')
                braceStart--;
            if (braceStart >= 0)
            {
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
                        Debug.LogWarning($"解析按钮失败: {e.Message}\nJSON: {jsonStr}");
                    }
                }
            }
        }

        // 普通文本消息
        AddMessage($"纯白: {aiReply}", false);
    }

    void AddMessage(string text, bool isPlayer)
    {
        GameObject prefab = isPlayer ? playerMessagePrefab : aiMessagePrefab;
        if (prefab == null)
        {
            Debug.LogError($"消息预制体未赋值！{(isPlayer ? "playerMessagePrefab" : "aiMessagePrefab")}");
            return;
        }

        GameObject msgObj = Instantiate(prefab, messageContainer);
        TMP_Text tmpText = msgObj.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
            tmpText.text = text;

        // 根据换行符数量动态调整高度
        int lineCount = text.Split('\n').Length;
        float lineHeight = 50f;          // 可根据实际字体大小调整，或动态获取
        float padding = 20f;             // 上下留白
        float totalHeight = lineCount * lineHeight + padding;

        RectTransform rect = msgObj.GetComponent<RectTransform>();
        if (rect != null)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);

        // 如果内部 Text 需要单独控制高度（可选）
        if (tmpText != null)
        {
            RectTransform textRect = tmpText.GetComponent<RectTransform>();
            if (textRect != null)
                textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, lineCount * lineHeight);
        }

        ScrollToBottom();
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

    void ScrollToBottom(float delay = 0.1f)
    {
        StartCoroutine(ScrollToBottomCoroutine(delay));
    }

    void CreateButton(ButtonData data)
    {
        if (buttonPrefab == null)
        {
            Debug.LogError("buttonPrefab 未赋值");
            return;
        }
        GameObject btnObj = Instantiate(buttonPrefab, messageContainer);
        Button btn = btnObj.GetComponent<Button>();
        TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
            btnText.text = data.label;
        btn.onClick.AddListener(() => ExecuteAction(data.action, data.@params));

        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContainer as RectTransform);
        ScrollToBottom(0.05f);
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
                if (TryGetSceneInfo(param.location, out string sceneName, out float x, out float y))
                {
                    AddMessage($"系统: 正在传送到 {param.location}...", false);
                    SceneDataManager.Instance.LoadScene(sceneName, (int)x, (int)y);
                }
                else
                {
                    AddMessage($"纯白: 我不认识「{param.location}」。我只知道家、回廊、原野、工坊、大厅、花地这些地方……", false);
                }
                break;
            case "open_workshop":
                Debug.Log("打开工坊");
                AddMessage("系统: 工坊已打开。", false);
                break;
            case "comfort":
                AddMessage("纯白轻轻握住你的手，银白色的发梢拂过你的掌心……", false);
                break;
            default:
                Debug.LogWarning($"未知动作: {action}");
                AddMessage($"纯白: 我好像不知道如何执行「{action}」……", false);
                break;
        }
    }

    [Serializable]
    public class ButtonData
    {
        public string type;
        public string label;
        public string action;
        public ActionParams @params;
    }

    [Serializable]
    public class ActionParams
    {
        public string location;
    }

    // 场景映射
    private readonly Dictionary<string, (string sceneName, float x, float y)> sceneMap =
        new Dictionary<string, (string, float, float)>
    {
        { "家", ("1_TheNestOfWarmLight", 0f, 0f) },
        { "村", ("1_TheNestOfWarmLight", 0f, 0f) },
        { "暖光之巢", ("1_TheNestOfWarmLight", 0f, 0f) },
        { "nest", ("1_TheNestOfWarmLight", 0f, 0f) },
        { "纯白回廊", ("2_TheArgentCorridor", 0f, 0f) },
        { "回廊", ("2_TheArgentCorridor", 0f, 0f) },
        { "corridor", ("2_TheArgentCorridor", 0f, 0f) },
        { "原野", ("3_TheVerdantMeadow", 0f, 100f) },
        { "草原", ("3_TheVerdantMeadow", 0f, 100f) },
        { "大树下", ("3_TheVerdantMeadow", 0f, 100f) },
        { "meadow", ("3_TheVerdantMeadow", 0f, 100f) },
        { "初萌原野", ("3_TheVerdantMeadow", 0f, 100f) },
        { "工坊", ("4_TheWorkshopOfPassion", 0f, 0f) },
        { "痴迷工坊", ("4_TheWorkshopOfPassion", 0f, 0f) },
        { "workshop", ("4_TheWorkshopOfPassion", 0f, 0f) },
        { "大厅", ("5_TheHallOfUniversalConcord", 0f, 0f) },
        { "共鸣大厅", ("5_TheHallOfUniversalConcord", 0f, 0f) },
        { "决战之地", ("5_TheHallOfUniversalConcord", 0f, 0f) },
        { "hall", ("5_TheHallOfUniversalConcord", 0f, 0f) },
        { "花地", ("6_TheStellarWish", 0f, 0f) },
        { "星愿花海", ("6_TheStellarWish", 0f, 0f) },
        { "stellar", ("6_TheStellarWish", 0f, 0f) },
    };

    private bool TryGetSceneInfo(string locationName, out string sceneName, out float x, out float y)
    {
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