using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景连通图管理 (静态)
/// </summary>
public static class ScenePathManager
{
    // 邻接表，key 场景名，value 可达的场景名列表
    private static Dictionary<string, List<string>> graph = new Dictionary<string, List<string>>();

    // 在游戏启动时调用一次初始化
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        BuildGraph();
    }

    private static void BuildGraph()
    {
        // 根据你提供的连接关系构建（注意场景命名要与实际场景名一致）
        // 你的连接: 12,13,23,24,25,26,34,45
        AddEdge("1_TheNestOfWarmLight", "2_TheArgentCorridor");
        AddEdge("1_TheNestOfWarmLight", "3_TheVerdantMeadow");
        AddEdge("2_TheArgentCorridor", "3_TheVerdantMeadow");
        AddEdge("2_TheArgentCorridor", "4_TheWorkshopOfPassion");
        AddEdge("2_TheArgentCorridor", "5_TheHallOfUniversalConcord");
        AddEdge("2_TheArgentCorridor", "6_TheStellarWish");
        AddEdge("3_TheVerdantMeadow", "4_TheWorkshopOfPassion");
        AddEdge("4_TheWorkshopOfPassion", "5_TheHallOfUniversalConcord");

        // 由于传送门是双向的，需要添加反向边
        // 例如 12 双向，上面的 AddEdge 会自动处理双向，因为调用了两次
    }

    private static void AddEdge(string a, string b)
    {
        if (!graph.ContainsKey(a)) graph[a] = new List<string>();
        if (!graph[a].Contains(b)) graph[a].Add(b);

        if (!graph.ContainsKey(b)) graph[b] = new List<string>();
        if (!graph[b].Contains(a)) graph[b].Add(a);
    }

    /// <summary>
    /// 使用 BFS 计算从 startScene 到 targetScene 的最短路径（场景名列表，包括起点和终点）
    /// </summary>
    public static List<string> GetShortestPath(string startScene, string targetScene)
    {
        if (startScene == targetScene)
            return new List<string> { startScene };

        Queue<string> queue = new Queue<string>();
        Dictionary<string, string> parent = new Dictionary<string, string>();
        HashSet<string> visited = new HashSet<string>();

        queue.Enqueue(startScene);
        visited.Add(startScene);
        parent[startScene] = null;

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            if (current == targetScene)
                break;

            if (!graph.ContainsKey(current)) continue;

            foreach (string neighbor in graph[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (!parent.ContainsKey(targetScene))
        {
            Debug.LogWarning($"无法找到从 {startScene} 到 {targetScene} 的路径");
            return null;
        }

        // 回溯路径
        List<string> path = new List<string>();
        string node = targetScene;
        while (node != null)
        {
            path.Add(node);
            node = parent[node];
        }
        path.Reverse();
        return path;
    }
}