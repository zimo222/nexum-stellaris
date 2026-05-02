using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class KnowledgeRetriever : MonoBehaviour
{
    [Header("知识库文件夹（StreamingAssets 下）")]
    public string knowledgeFolder = "Knowledge";

    private List<KnowledgeEntry> entries = new List<KnowledgeEntry>();

    [System.Serializable]
    public class KnowledgeEntry
    {
        public string key;      // 关键词/主题
        public string content;  // 详细描述
    }

    void Awake()
    {
        LoadAllKnowledge();
    }

    void LoadAllKnowledge()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, knowledgeFolder);
        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning($"知识库文件夹不存在: {fullPath}");
            return;
        }

        string[] files = Directory.GetFiles(fullPath, "*.txt");
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                // 按冒号拆分，格式：关键词: 描述内容
                int colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    string key = line.Substring(0, colonIdx).Trim();
                    string content = line.Substring(colonIdx + 1).Trim();
                    entries.Add(new KnowledgeEntry { key = key, content = content });
                }
                else
                {
                    // 如果没有冒号，就把整行作为内容，key 设为空（暂不处理）
                    Debug.LogWarning($"忽略无效的知识行: {line}");
                }
            }
        }
        Debug.Log($"加载了 {entries.Count} 条知识条目");
    }

    /// <summary>
    /// 根据用户输入，检索最相关的知识（简单关键词匹配）
    /// </summary>
    public string RetrieveRelevantContext(string userMessage, int maxResults = 3)
    {
        if (entries.Count == 0) return "";

        var scored = new List<(KnowledgeEntry entry, int score)>();
        foreach (var entry in entries)
        {
            int score = 0;
            // 用户消息中包含知识库的 key（如“生日”）
            if (userMessage.Contains(entry.key))
            {
                score += 10; // 基础分
            }
            // 可选：如果用户消息中也包含内容中的关键短语，加分
            // 但要避免内容太长导致误判，可以只检查前20字
            string shortContent = entry.content.Length > 50 ? entry.content.Substring(0, 50) : entry.content;
            if (userMessage.Contains(shortContent))
                score += 5;

            if (score > 0)
                scored.Add((entry, score));
        }

        if (scored.Count == 0) return "";

        var top = scored.OrderByDescending(x => x.score).Take(maxResults).ToList();
        string context = "以下是一些相关设定：\n";
        foreach (var t in top)
        {
            context += $"- {t.entry.key}: {t.entry.content}\n";
        }
        Debug.Log(context);
        return context;
    }
}