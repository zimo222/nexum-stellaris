using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class LongTermMemory : MonoBehaviour
{
    [Header("配置")]
    public string memoryFileName = "long_term_memory.json";
    public int topK = 3;
    public float similarityThreshold = 0.5f;

    private List<MemoryEntry> memories = new List<MemoryEntry>();
    private string memoryPath;
    private string apiKey;  // 百炼 API Key

    [System.Serializable]
    public class MemoryEntry
    {
        public string id;
        public string text;
        public List<float> embedding;
        public long timestamp;

        public MemoryEntry(string text, float[] emb)
        {
            id = Guid.NewGuid().ToString();
            this.text = text;
            embedding = (emb != null ? emb.ToList() : null);
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    [System.Serializable]
    private class MemorySaveData
    {
        public List<MemoryEntry> memories;
    }

    void Awake()
    {
        LoadApiKey();
        memoryPath = Path.Combine(Application.persistentDataPath, memoryFileName);
        LoadMemories();
    }

    private void LoadApiKey()
    {
        TextAsset keyAsset = Resources.Load<TextAsset>("bailian_key");
        if (keyAsset != null)
        {
            apiKey = keyAsset.text.Trim();
            if (string.IsNullOrEmpty(apiKey))
                Debug.LogError("bailian_key.txt 内容为空！");
            else
                Debug.Log("成功加载百炼 API Key");
        }
        else
        {
            Debug.LogError("未找到 Resources/bailian_key.txt，请在 Assets/Resources 下创建该文件，内容为百炼 API Key");
        }
    }

    // ------------------- 公开接口 -------------------
    public IEnumerator AddMemory(string text, Action<bool> callback = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key 缺失，无法生成记忆向量");
            callback?.Invoke(false);
            yield break;
        }

        // 简单去重：如果已有相似记忆（相似度>0.9）则跳过
        float[] newVec = null;
        yield return GetEmbeddingCoroutine(text, (result) => newVec = result);
        if (newVec == null || newVec.Length == 0)
        {
            Debug.LogError($"记忆向量生成失败：{text}");
            callback?.Invoke(false);
            yield break;
        }

        // 去重检查
        foreach (var mem in memories)
        {
            if (mem.embedding == null || mem.embedding.Count == 0) continue;
            float[] oldVec = mem.embedding.ToArray();
            float sim = CosineSimilarity(newVec, oldVec);
            if (sim > 0.9f)
            {
                Debug.Log($"记忆已存在，跳过添加：{text}");
                callback?.Invoke(true);
                yield break;
            }
        }

        memories.Add(new MemoryEntry(text, newVec));
        SaveMemories();
        Debug.Log($"新增记忆：{text}");
        callback?.Invoke(true);
    }

    public IEnumerator Recall(string query, Action<List<MemoryEntry>> callback, int? customTopK = null)
    {
        if (memories.Count == 0)
        {
            callback?.Invoke(new List<MemoryEntry>());
            yield break;
        }

        float[] queryVec = null;
        yield return GetEmbeddingCoroutine(query, (result) => queryVec = result);
        if (queryVec == null || queryVec.Length == 0)
        {
            Debug.LogWarning("查询向量生成失败，无法检索记忆");
            callback?.Invoke(new List<MemoryEntry>());
            yield break;
        }

        int k = customTopK ?? topK;
        var scored = new List<(MemoryEntry entry, float score)>();
        foreach (var mem in memories)
        {
            if (mem.embedding == null || mem.embedding.Count == 0) continue;
            float[] vec = mem.embedding.ToArray();
            float sim = CosineSimilarity(queryVec, vec);
            if (sim >= similarityThreshold)
                scored.Add((mem, sim));
        }
        var results = scored.OrderByDescending(x => x.score).Take(k).Select(x => x.entry).ToList();
        callback?.Invoke(results);
    }

    public void ClearAllMemories()
    {
        memories.Clear();
        SaveMemories();
        Debug.Log("所有长期记忆已清除");
    }

    // ------------------- 内部实现 -------------------
    private void LoadMemories()
    {
        if (File.Exists(memoryPath))
        {
            string json = File.ReadAllText(memoryPath);
            var data = JsonConvert.DeserializeObject<MemorySaveData>(json);
            if (data != null && data.memories != null)
            {
                memories = data.memories;
                Debug.Log($"加载了 {memories.Count} 条长期记忆");
                return;
            }
        }
        memories = new List<MemoryEntry>();
        Debug.Log("没有找到长期记忆文件，将创建新记忆库");
    }

    private void SaveMemories()
    {
        var data = new MemorySaveData { memories = memories };
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(memoryPath, json);
    }

    private IEnumerator GetEmbeddingCoroutine(string text, Action<float[]> callback)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            callback?.Invoke(null);
            yield break;
        }

        string url = "https://dashscope.aliyuncs.com/compatible-mode/v1/embeddings";
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            var payload = new { model = "text-embedding-v4", input = text };
            string jsonBody = JsonConvert.SerializeObject(payload);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            request.certificateHandler = new BypassCertificate();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<EmbeddingResponse>(request.downloadHandler.text);
                    if (response?.data != null && response.data.Count > 0 && response.data[0].embedding != null)
                    {
                        callback?.Invoke(response.data[0].embedding);
                        yield break;
                    }
                }
                catch (Exception ex) { Debug.LogError($"解析失败: {ex.Message}"); }
            }
            else
            {
                Debug.LogError($"请求失败: {request.error}");
            }
            callback?.Invoke(null);
        }
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;
        float dot = 0f, magA = 0f, magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0f || magB == 0f) return 0f;
        return dot / (float)(Math.Sqrt(magA) * Math.Sqrt(magB));
    }

    [System.Serializable]
    private class EmbeddingResponse
    {
        public List<EmbeddingData> data;
    }
    [System.Serializable]
    private class EmbeddingData
    {
        public float[] embedding;
    }
}