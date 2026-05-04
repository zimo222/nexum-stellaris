using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static LongTermMemory;

[System.Serializable]
public class DeepSeekRequestData
{
    public string model;
    public List<DeepSeekMessage> messages;
    public bool stream = false;
}

[System.Serializable]
public class DeepSeekMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class DeepSeekResponseData
{
    public List<DeepSeekChoice> choices;
}

[System.Serializable]
public class DeepSeekChoice
{
    public DeepSeekMessage message;
}

public class UnifiedAIManager : MonoBehaviour
{
    [Header("API 配置")]
    [SerializeField] private string deepSeekApiKey;
    private const string DEEPSEEK_API_URL = "https://api.deepseek.com/v1/chat/completions";
    private const float API_TIMEOUT_SECONDS = 15f;
    [TextArea(3, 5)]
    [SerializeField] private string apiSystemPrompt = "你是游戏中的AI伙伴，性格友好、乐于助人。请用中文简短回复。";

    [Header("本地模型配置")]
    [SerializeField] private LLMUnity.LLMAgent localLLMAgent;

    [Header("知识库")]
    public VectorKnowledgeBase vectorKB;

    [Header("长期记忆")]
    public LongTermMemory longTermMemory;

    [Header("对话上下文设置")]
    public int maxContextMessages = 10;
    public int memoryExtractionInterval = 5;

    private List<DeepSeekMessage> conversationHistory = new List<DeepSeekMessage>();
    private int dialogueRoundCount = 0;
    private int lastExtractionRound = 0;
    private string historyFilePath;

    private bool isApiAvailable = false;
    private static UnifiedAIManager _instance;
    public static UnifiedAIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnifiedAIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("UnifiedAIManager");
                    _instance = go.AddComponent<UnifiedAIManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadApiKey();
        historyFilePath = Path.Combine(Application.persistentDataPath, "conversation_history.json");
        LoadConversationHistory();
    }

    private void LoadApiKey()
    {
        TextAsset keyAsset = Resources.Load<TextAsset>("deepseek_key");
        if (keyAsset != null)
        {
            deepSeekApiKey = keyAsset.text.Trim();
            if (string.IsNullOrEmpty(deepSeekApiKey))
                Debug.LogError("deepseek_key.txt 内容为空");
        }
        else
        {
            Debug.LogError("未找到 Resources/deepseek_key.txt，API 调用将失败");
        }
    }

    private void LoadConversationHistory()
    {
        if (File.Exists(historyFilePath))
        {
            string json = File.ReadAllText(historyFilePath);
            try
            {
                var history = JsonConvert.DeserializeObject<List<DeepSeekMessage>>(json);
                if (history != null)
                {
                    conversationHistory = history;
                    Debug.Log($"加载对话历史，共 {conversationHistory.Count} 条消息");
                    dialogueRoundCount = 0;
                    foreach (var msg in conversationHistory)
                        if (msg.role == "user") dialogueRoundCount++;
                    lastExtractionRound = dialogueRoundCount;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"加载对话历史失败: {e.Message}");
            }
        }
        else
        {
            Debug.Log("没有找到历史对话文件，将创建新对话历史");
        }
    }

    private void SaveConversationHistory()
    {
        string json = JsonConvert.SerializeObject(conversationHistory, Formatting.Indented);
        File.WriteAllText(historyFilePath, json);
    }

    void Start()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("网络不可用，将直接使用本地模型。");
            isApiAvailable = false;
        }
        else
        {
            isApiAvailable = true;
        }
    }

    public void SendMessageToAI(string userMessage, Action<string> onSuccess, Action onFallbackToLocal = null)
    {
        if (isApiAvailable)
        {
            StartCoroutine(CallDeepSeekAPI(userMessage, onSuccess, (errorMsg) => {
                Debug.LogWarning($"API调用失败: {errorMsg}，回退到本地模型。");
                onFallbackToLocal?.Invoke();
                isApiAvailable = false;
                SendMessageToLocalModel(userMessage, onSuccess);
            }));
        }
        else
        {
            SendMessageToLocalModel(userMessage, onSuccess);
        }
    }

    private IEnumerator CallDeepSeekAPI(string msg, Action<string> onSuccess, Action<string> onError)
    {
        // 检索知识库
        List<(VectorKnowledgeBase.KnowledgeEntry entry, float score)> relevant = null;
        if (vectorKB != null)
        {
            bool searchComplete = false;
            yield return StartCoroutine(vectorKB.SearchRoutine(msg, (results) => {
                relevant = results;
                searchComplete = true;
            }));
            while (!searchComplete) yield return null;
        }

        // 检索长期记忆
        List<MemoryEntry> relevantMemories = null;
        if (longTermMemory != null)
        {
            bool recallComplete = false;
            yield return StartCoroutine(longTermMemory.Recall(msg, (memories) => {
                relevantMemories = memories;
                recallComplete = true;
            }));
            while (!recallComplete) yield return null;
        }

        // 系统提示
        string finalSystemPrompt = apiSystemPrompt;
        if (relevant != null && relevant.Count > 0)
        {
            finalSystemPrompt += "\n\n【相关知识】\n";
            foreach (var r in relevant)
                finalSystemPrompt += $"- {r.entry.key}: {r.entry.content}\n";
        }
        if (relevantMemories != null && relevantMemories.Count > 0)
        {
            finalSystemPrompt += "\n\n【长期记忆】\n";
            foreach (var mem in relevantMemories)
                finalSystemPrompt += $"- {mem.text}\n";
        }

        // 构建消息历史
        var messages = new List<DeepSeekMessage>();
        messages.Add(new DeepSeekMessage { role = "system", content = finalSystemPrompt });

        int startIdx = Math.Max(0, conversationHistory.Count - maxContextMessages);
        for (int i = startIdx; i < conversationHistory.Count; i++)
            messages.Add(conversationHistory[i]);

        messages.Add(new DeepSeekMessage { role = "user", content = msg });

        // 请求
        var requestData = new DeepSeekRequestData
        {
            model = "deepseek-chat",
            messages = messages
        };
        string jsonData = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(DEEPSEEK_API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {deepSeekApiKey}");
            request.timeout = (int)API_TIMEOUT_SECONDS;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<DeepSeekResponseData>(request.downloadHandler.text);
                    if (response != null && response.choices != null && response.choices.Count > 0)
                    {
                        string aiReply = response.choices[0].message.content;
                        onSuccess?.Invoke(aiReply);

                        // 更新对话历史
                        conversationHistory.Add(new DeepSeekMessage { role = "user", content = msg });
                        conversationHistory.Add(new DeepSeekMessage { role = "assistant", content = aiReply });
                        dialogueRoundCount++;

                        while (conversationHistory.Count > maxContextMessages * 2)
                            conversationHistory.RemoveAt(0);

                        SaveConversationHistory();

                        // 定期提取长期记忆
                        if (dialogueRoundCount - lastExtractionRound >= memoryExtractionInterval)
                        {
                            StartCoroutine(ExtractMemoriesFromRecentConversations());
                            lastExtractionRound = dialogueRoundCount;
                        }
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"解析JSON错误: {ex.Message}");
                    yield break;
                }
            }
            onError?.Invoke($"请求失败: {request.error}");
        }
    }

    private IEnumerator ExtractMemoriesFromRecentConversations()
    {
        if (longTermMemory == null) yield break;
        if (conversationHistory.Count == 0) yield break;

        // 计算需要提取的对话范围
        int totalPairs = conversationHistory.Count / 2;
        int startPairIndex = Math.Max(0, totalPairs - memoryExtractionInterval);
        int startMsgIndex = startPairIndex * 2;
        if (startMsgIndex >= conversationHistory.Count) yield break;

        StringBuilder recentDialog = new StringBuilder();
        for (int i = startMsgIndex; i < conversationHistory.Count; i++)
        {
            string role = conversationHistory[i].role == "user" ? "用户" : "纯白";
            recentDialog.AppendLine($"{role}：{conversationHistory[i].content}");
        }

        string extractionPrompt = $@"请从以下对话中提取出值得长期记住的事实信息（例如玩家的生日、喜好、重要约定、性格特征等）。每条事实用一句话简洁描述。输出为一个 JSON 数组，例如：[""玩家喜欢红色"", ""玩家怕黑""]。如果没有值得记住的信息，输出空数组 []。只输出 JSON，不要任何额外文字。

对话内容：
{recentDialog.ToString()}";

        var messages = new List<DeepSeekMessage>
    {
        new DeepSeekMessage { role = "system", content = "你是一个记忆提取助手，只输出 JSON 数组。" },
        new DeepSeekMessage { role = "user", content = extractionPrompt }
    };

        var requestData = new DeepSeekRequestData
        {
            model = "deepseek-chat",
            messages = messages,
            stream = false
        };
        string jsonData = JsonUtility.ToJson(requestData);

        string responseText = "";
        bool requestSuccess = false;

        using (UnityWebRequest request = new UnityWebRequest(DEEPSEEK_API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {deepSeekApiKey}");
            request.timeout = 15;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                responseText = request.downloadHandler.text;
                requestSuccess = true;
            }
            else
            {
                Debug.LogWarning($"批量记忆提取请求失败: {request.error}");
            }
        }

        if (!requestSuccess) yield break;

        // 解析完整响应，提取 content 字段
        string contentText = "";
        try
        {
            var chatResponse = JsonUtility.FromJson<DeepSeekResponseData>(responseText);
            if (chatResponse != null && chatResponse.choices != null && chatResponse.choices.Count > 0)
                contentText = chatResponse.choices[0].message.content;
            else
                Debug.LogWarning("响应中没有 choices");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"解析响应结构失败: {ex.Message}\n响应: {responseText}");
            yield break;
        }

        if (string.IsNullOrEmpty(contentText))
        {
            Debug.LogWarning("提取的记忆 content 为空");
            yield break;
        }

        // 解析 content 中的 JSON 数组
        List<string> facts = null;
        try
        {
            facts = JsonConvert.DeserializeObject<List<string>>(contentText);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"解析记忆数组失败: {ex.Message}\ncontent: {contentText}");
            yield break;
        }

        if (facts != null && facts.Count > 0)
        {
            foreach (string fact in facts)
            {
                yield return StartCoroutine(longTermMemory.AddMemory(fact));
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log($"批量提取长期记忆完成，新增 {facts.Count} 条记忆");
        }
        else
        {
            Debug.Log("批量记忆提取: 没有值得记住的事实");
        }
    }

    private async void SendMessageToLocalModel(string userMessage, Action<string> onSuccess)
    {
        if (localLLMAgent == null)
        {
            Debug.LogError("本地 LLMAgent 未引用！");
            onSuccess?.Invoke("[系统] AI助理离线，请联系管理员。");
            return;
        }
        await localLLMAgent.Chat(userMessage, (string reply) => {
            onSuccess?.Invoke(reply);
        });
    }

    public string GetApiKey() => deepSeekApiKey;

    public void ClearConversationHistory()
    {
        conversationHistory.Clear();
        dialogueRoundCount = 0;
        lastExtractionRound = 0;
        SaveConversationHistory();
        Debug.Log("对话历史已清空并保存");
    }
}