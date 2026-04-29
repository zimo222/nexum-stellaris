using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 管理所有 Scene 类型传送门的注册与查询
/// </summary>
public class SceneGraphManager : MonoBehaviour
{
    public static SceneGraphManager Instance { get; private set; }

    // key: 当前场景名, value: 该场景中所有的传送门信息列表
    private Dictionary<string, List<ScenePortalInfo>> portalsByScene = new Dictionary<string, List<ScenePortalInfo>>();

    // 用于存储传送门位置信息的结构
    public class ScenePortalInfo
    {
        public string targetScene;
        public Vector3 position;
        public string portalQuestId;   // 传送门的前置任务ID (questId)
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 监听场景卸载事件，清除该场景的所有注册信息（因为场景物体被销毁时会自动注销，但为了防止残留，可以主动清理）
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        // 场景卸载时，移除该场景的所有传送门注册
        string sceneName = scene.name;
        if (portalsByScene.ContainsKey(sceneName))
        {
            portalsByScene.Remove(sceneName);
            Debug.Log($"[SceneGraphManager] 移除场景 {sceneName} 的传送门注册");
        }
    }

    /// <summary>
    /// 注册一个传送门 (由 QuestTriggerZone 调用)
    /// </summary>
    public void RegisterPortal(string currentScene, string targetScene, Vector3 position, string questId)
    {
        if (!portalsByScene.ContainsKey(currentScene))
            portalsByScene[currentScene] = new List<ScenePortalInfo>();

        var info = new ScenePortalInfo
        {
            targetScene = targetScene,
            position = position,
            portalQuestId = questId
        };

        // 避免重复注册同一位置（简单按位置去重，也可不处理）
        if (!portalsByScene[currentScene].Exists(p => p.position == position))
            portalsByScene[currentScene].Add(info);
    }

    /// <summary>
    /// 注销一个传送门
    /// </summary>
    public void UnregisterPortal(string currentScene, Vector3 position)
    {
        if (portalsByScene.TryGetValue(currentScene, out var list))
        {
            list.RemoveAll(p => p.position == position);
            if (list.Count == 0)
                portalsByScene.Remove(currentScene);
        }
    }

    /// <summary>
    /// 获取当前场景中，能够通往 targetScene 的传送门，并返回离玩家最近的那个
    /// </summary>
    public Transform GetNearestPortalToTarget(string currentScene, string targetScene, Vector3 playerPos)
    {
        if (!portalsByScene.TryGetValue(currentScene, out var portals))
            return null;

        // 筛选目标场景匹配的传送门
        var validPortals = portals.FindAll(p => p.targetScene == targetScene);
        if (validPortals.Count == 0)
            return null;

        // 找出距离最近的
        ScenePortalInfo nearest = null;
        float minDist = float.MaxValue;
        foreach (var portal in validPortals)
        {
            float dist = Vector3.Distance(playerPos, portal.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = portal;
            }
        }

        // 返回该传送门的 Transform (需要额外存储，这里简化：通过位置查找场景中的物体)
        // 更好的办法是注册时直接存 Transform，但我们只能存位置，可以通过 Physics2D.OverlapPoint 查找
        if (nearest != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(nearest.position, 0.1f);
            foreach (var hit in hits)
            {
                QuestTriggerZone zone = hit.GetComponent<QuestTriggerZone>();
                if (zone != null && zone.triggerType == QuestTriggerZone.TriggerType.Scene && zone.targetSceneName == nearest.targetScene)
                    return zone.transform;
            }
        }
        return null;
    }
}