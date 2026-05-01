using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using LLMUnity;

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
    // === 配置区域 ===
    [Header("API 配置")]
    [SerializeField] private string deepSeekApiKey; // 请替换为你的真实API Key
    private const string DEEPSEEK_API_URL = "https://api.deepseek.com/v1/chat/completions";
    private const float API_TIMEOUT_SECONDS = 15f;
    [TextArea(3, 5)]          // 让输入框变大，方便写长文本
    [SerializeField] private string apiSystemPrompt = "你是游戏中的AI伙伴，性格友好、乐于助人。请用中文简短回复。";

    [Header("本地模型配置")]
    [SerializeField] private LLMAgent localLLMAgent; // 引用场景中的 LLMAgent

    // === 内部状态 ===
    private bool isApiAvailable = false; // API可用性标志

    // 1. 单例实例
    private static UnifiedAIManager _instance;
    public static UnifiedAIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 如果场景中不存在，尝试在资源中查找（一般不会发生）
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
        // 单例检查：如果已经存在另一个实例，销毁当前对象
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        // 可选：让管理器在场景切换时不被销毁（保持 AI 对话状态）
        DontDestroyOnLoad(gameObject);

        // 保持原有的 LLM 和 LLMAgent 组件初始化（如果有）
        // 确保已有的 DeepSeek API Key 等配置不受影响

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
        // 启动时快速检测网络是否可用
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("网络不可用，将直接使用本地模型。");
            isApiAvailable = false;
        }
        else
        {
            // 如果有网络，默认认为API可用，实际使用时会尝试调用
            isApiAvailable = true;
        }
    }

    /// <summary>
    /// 外部统一调用的入口
    /// </summary>
    /// <param name="userMessage">用户输入的消息</param>
    /// <param name="onSuccess">成功获得回复后的回调</param>
    /// <param name="onFallbackToLocal">当API无法使用，即将切换到本地模式时的回调（可选）</param>
    public void SendMessageToAI(string userMessage, Action<string> onSuccess, Action onFallbackToLocal = null)
    {
        // 1. 优先尝试API
        if (isApiAvailable)
        {
            StartCoroutine(CallDeepSeekAPI(userMessage, onSuccess, (errorMsg) => {
                Debug.LogWarning($"API调用失败: {errorMsg}， 回退到本地模型。");
                onFallbackToLocal?.Invoke();
                // 回退策略：后续请求直接尝试本地，可以暂时将API设为不可用
                isApiAvailable = false;
                SendMessageToLocalModel(userMessage, onSuccess);
            }));
        }
        else
        {
            // 初次或无网络，直接调用本地模型
            SendMessageToLocalModel(userMessage, onSuccess);
        }
    }

    // === API 调用相关 ===
    private IEnumerator CallDeepSeekAPI(string msg, Action<string> onSuccess, Action<string> onError)
    {
        List<DeepSeekMessage> messages = new List<DeepSeekMessage>();

        // 如果设置了系统提示词，先添加 system 消息
        if (!string.IsNullOrEmpty(apiSystemPrompt))
        {
            messages.Add(new DeepSeekMessage { role = "system", content = apiSystemPrompt });
        }
        // 再添加用户消息
        messages.Add(new DeepSeekMessage { role = "user", content = msg });

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

            // 使用协程支持超时控制
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
            // 处理各种错误情况
            onError?.Invoke($"请求失败: {request.error}");
        }
    }

    // === 本地模型调用 ===
    private async void SendMessageToLocalModel(string userMessage, Action<string> onSuccess)
    {
        if (localLLMAgent == null)
        {
            Debug.LogError("本地 LLMAgent 未引用！");
            onSuccess?.Invoke("[系统] AI助理离线，请联系管理员。");
            return;
        }

        // 确保已有对话历史，这里简单起见，每次都传入用户消息
        // 如果你想保留多轮对话历史，需要改造 localLLMAgent.Chat 的第二个参数
        await localLLMAgent.Chat(userMessage, (string reply) => {
            onSuccess?.Invoke(reply);
        });
    }
}