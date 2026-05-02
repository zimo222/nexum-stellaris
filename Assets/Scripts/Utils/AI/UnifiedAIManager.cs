using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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
    public VectorKnowledgeBase vectorKB;   // 使用百炼 Embedding 的知识库

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
        // 1. 从知识库检索相关内容（使用百炼 Embedding）
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

        // 2. 构建最终的系统提示词
        string finalSystemPrompt = apiSystemPrompt;
        if (relevant != null && relevant.Count > 0)
        {
            finalSystemPrompt += "\n\n【相关知识】\n";
            foreach (var r in relevant)
            {
                finalSystemPrompt += $"- {r.entry.key}: {r.entry.content}\n";
                Debug.Log(r.entry);
            }
        }


        // 3. 构建消息列表
        var messages = new List<DeepSeekMessage>();
        messages.Add(new DeepSeekMessage { role = "system", content = finalSystemPrompt });
        messages.Add(new DeepSeekMessage { role = "user", content = msg });

        var requestData = new DeepSeekRequestData
        {
            model = "deepseek-chat",
            messages = messages
        };
        string jsonData = JsonUtility.ToJson(requestData);

        // 4. 发送请求
        using (UnityWebRequest request = new UnityWebRequest(DEEPSEEK_API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {deepSeekApiKey}");
            request.timeout = (int)API_TIMEOUT_SECONDS;

            // 可选：开发阶段若遇 SSL 错误，取消注释下面一行（需要定义 BypassCertificate 类）
            // request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<DeepSeekResponseData>(request.downloadHandler.text);
                    if (response != null && response.choices != null && response.choices.Count > 0)
                    {
                        onSuccess?.Invoke(response.choices[0].message.content);
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

    // 供其他组件获取 API Key（例如 VectorKnowledgeBase 需要读取 deepseek API Key，但实际不再需要）
    public string GetApiKey() => deepSeekApiKey;
}