using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class VectorKnowledgeBase : MonoBehaviour
{
    [Header("配置")]
    public string knowledgeFolder = "Knowledge";
    public string cacheFileName = "knowledge_vectors.json";
    public int topK = 3;
    public float similarityThreshold = 0.5f;

    private List<KnowledgeEntry> entries = new List<KnowledgeEntry>();
    private List<float[]> entryVectors = new List<float[]>();
    private string cachePath;
    private string apiKey;
    private int currentChapter = 0;
    private int currentAct = 0;

    private static readonly Regex FileNameRegex = new Regex(@"^Chapter(\d{3})_(\d{3})\.txt$", RegexOptions.Compiled);

    [System.Serializable]
    public class KnowledgeEntry
    {
        public string key;
        public string content;
    }

    [System.Serializable]
    public class CacheData
    {
        public List<KnowledgeEntry> entries;
        public List<List<float>> vectors;
    }

    void Awake()
    {
        LoadApiKey();
        cachePath = Path.Combine(Application.persistentDataPath, cacheFileName);
    }

    IEnumerator Start()
    {
        yield return StartCoroutine(SetProgressCoroutine(0, 0));
    }

    public void SetProgress(int chapter, int act)
    {
        if (chapter == currentChapter && act == currentAct) return;
        StartCoroutine(SetProgressCoroutine(chapter, act));
    }

    public IEnumerator SearchRoutine(string query, Action<List<(KnowledgeEntry entry, float score)>> callback, int? customTopK = null)
    {
        if (entries.Count == 0 || entryVectors.Count == 0)
        {
            callback?.Invoke(new List<(KnowledgeEntry, float)>());
            yield break;
        }

        float[] queryVec = null;
        yield return StartCoroutine(GetEmbeddingCoroutine(query, (result) => queryVec = result));
        if (queryVec == null || queryVec.Length == 0)
        {
            Debug.LogWarning("查询向量生成失败");
            callback?.Invoke(new List<(KnowledgeEntry, float)>());
            yield break;
        }

        int k = customTopK ?? topK;
        var scored = new List<(KnowledgeEntry entry, float score)>();
        int maxIdx = Math.Min(entries.Count, entryVectors.Count);
        for (int i = 0; i < maxIdx; i++)
        {
            if (entryVectors[i] == null || entryVectors[i].Length == 0) continue;
            float sim = CosineSimilarity(queryVec, entryVectors[i]);
            if (sim >= similarityThreshold)
                scored.Add((entries[i], sim));
        }
        var results = scored.OrderByDescending(x => x.score).Take(k).ToList();
        callback?.Invoke(results);
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

    private IEnumerator SetProgressCoroutine(int chapter, int act)
    {
        currentChapter = chapter;
        currentAct = act;
        Debug.Log($"剧情进度更新：第 {chapter} 章 第 {act} 幕，重新加载知识库...");

        entries.Clear();
        entryVectors.Clear();

        string folder = Path.Combine(Application.streamingAssetsPath, knowledgeFolder);
        if (!Directory.Exists(folder))
        {
            Debug.LogError($"知识库文件夹不存在: {folder}");
            yield break;
        }

        var allFiles = Directory.GetFiles(folder, "*.txt")
            .Select(f => new { Path = f, Name = Path.GetFileName(f) })
            .Where(f => FileNameRegex.IsMatch(f.Name))
            .Select(f => new { FilePath = f.Path, Match = FileNameRegex.Match(f.Name) })
            .Select(m => new { m.FilePath, Chapter = int.Parse(m.Match.Groups[1].Value), Act = int.Parse(m.Match.Groups[2].Value) })
            .OrderBy(f => f.Chapter).ThenBy(f => f.Act)
            .ToList();

        var filesToLoad = allFiles.Where(f => f.Chapter < currentChapter || (f.Chapter == currentChapter && f.Act <= currentAct)).ToList();

        if (filesToLoad.Count == 0)
        {
            Debug.LogWarning($"没有符合条件的知识文件（章节<={currentChapter}，幕<={currentAct}）");
            yield break;
        }

        foreach (var fileInfo in filesToLoad)
        {
            yield return StartCoroutine(LoadKnowledgeFromFile(fileInfo.FilePath));
            yield return null;
        }

        if (entries.Count == 0)
        {
            Debug.LogWarning($"加载后没有有效知识条目");
            yield break;
        }

        yield return StartCoroutine(GenerateAllVectors());
        SaveCache();
        Debug.Log($"知识库已更新：共 {entries.Count} 条知识（章节≤{currentChapter}，章节={currentChapter}时幕≤{currentAct}）");
    }

    private IEnumerator LoadKnowledgeFromFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            int colonIdx = line.IndexOf(':');
            if (colonIdx > 0)
            {
                string key = line.Substring(0, colonIdx).Trim();
                string content = line.Substring(colonIdx + 1).Trim();
                entries.Add(new KnowledgeEntry { key = key, content = content });
            }
            else
            {
                Debug.LogWarning($"忽略无效行（缺少冒号）: {line} (文件: {Path.GetFileName(filePath)})");
            }
        }
        yield return null;
    }

    private IEnumerator GenerateAllVectors()
    {
        entryVectors.Clear();
        int failedCount = 0;
        foreach (var entry in entries)
        {
            string textToEmbed = $"{entry.key}: {entry.content}";
            float[] vector = null;
            yield return StartCoroutine(GetEmbeddingCoroutine(textToEmbed, (result) => vector = result));
            if (vector != null && vector.Length > 0)
                entryVectors.Add(vector);
            else
            {
                Debug.LogError($"获取向量失败: {entry.key}，将添加零向量占位");
                entryVectors.Add(new float[0]);
                failedCount++;
            }
            yield return new WaitForSeconds(0.1f);
        }
        if (entryVectors.Count != entries.Count)
            Debug.LogError($"严重错误：向量数量 {entryVectors.Count} 与条目数 {entries.Count} 不一致！");
        else
            Debug.Log($"向量生成完成，成功 {entries.Count - failedCount} 条，失败 {failedCount} 条");
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
                    Debug.LogError($"Embedding 响应数据为空: {request.downloadHandler.text}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"解析 Embedding 响应失败: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError($"百炼 Embedding 请求失败: {request.error}\n响应内容: {request.downloadHandler.text}");
            }
            callback?.Invoke(null);
        }
    }

    private float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0f;
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

    private void SaveCache()
    {
        var cache = new CacheData
        {
            entries = entries,
            vectors = entryVectors.Select(v => v?.ToList() ?? new List<float>()).ToList()
        };
        string json = JsonConvert.SerializeObject(cache, Formatting.Indented);
        File.WriteAllText(cachePath, json);
        Debug.Log($"知识向量缓存已保存至: {cachePath}");
    }

#if UNITY_EDITOR
    [ContextMenu("强制重建缓存（根据当前进度）")]
    public void ForceRebuildCache()
    {
        if (Application.isPlaying)
        {
            Debug.Log("强制重建缓存，将重新加载当前进度的知识库");
            StartCoroutine(SetProgressCoroutine(currentChapter, currentAct));
        }
        else
        {
            Debug.LogWarning("请在 Play 模式下使用此功能");
        }
    }
#endif

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

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}