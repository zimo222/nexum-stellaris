using System.Collections.Generic;
using UnityEngine;
// 在类顶部增加 using
using UnityEngine.SceneManagement;

public class QuestArrowGuide : MonoBehaviour
{
    [Header("箭头UI")]
    public RectTransform arrowImage;
    public float radius = 150f;

    private Transform currentTarget;
    private Transform playerTransform;
    private string currentTrackedQuestId;
    private Vector3 lastPlayerPos;   // 用于减少频繁计算

    private void Start()
    {
        if (arrowImage == null)
        {
            Debug.LogError("请将箭头图片的 RectTransform 赋值给 arrowImage");
            enabled = false;
            return;
        }

        arrowImage.gameObject.SetActive(false);
        radius = Mathf.Min(Screen.width, Screen.height) * 0.3f;

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTrackedQuestChanged += OnTrackedQuestChanged;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogError("未找到玩家物体");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnTrackedQuestChanged(string questId)
    {
        currentTrackedQuestId = questId;
        RefreshTarget();
    }

    // 场景加载完成后刷新目标（确保新场景的传送门被找到）
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshTarget();
    }

    private void RefreshTarget()
    {
        currentTarget = null;

        if (string.IsNullOrEmpty(currentTrackedQuestId))
        {
            arrowImage.gameObject.SetActive(false);
            return;
        }

        // 获取任务数据
        if (!GameDataManager.Instance.QuestDict.TryGetValue(currentTrackedQuestId, out var questData))
        {
            arrowImage.gameObject.SetActive(false);
            return;
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string targetScene = questData.questSceneName;   // 任务所在场景

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning($"任务 {currentTrackedQuestId} 未配置 questSceneName");
            arrowImage.gameObject.SetActive(false);
            return;
        }

        // 情况1：目标场景就是当前场景
        if (currentScene == targetScene)
        {
            // 查找当前场景中的任务触发器
            Transform trigger = FindQuestTriggerInScene(currentTrackedQuestId);
            if (trigger != null)
            {
                currentTarget = trigger;
                arrowImage.gameObject.SetActive(true);
            }
            else
            {
                // 可能触发器还未生成？例如任务需要先解锁
                arrowImage.gameObject.SetActive(false);
                Debug.Log($"在当前场景 {currentScene} 未找到任务 {currentTrackedQuestId} 的触发器");
            }
            return;
        }

        // 情况2：目标场景不在当前场景，需要路径导航
        List<string> path = ScenePathManager.GetShortestPath(currentScene, targetScene);
        if (path == null || path.Count < 2)
        {
            arrowImage.gameObject.SetActive(false);
            return;
        }

        // 需要前往的第一个目标场景是 path[1]
        string nextScene = path[1];
        // 在当前场景中找到通往 nextScene 的传送门
        Transform portal = FindPortalToScene(nextScene);
        if (portal != null)
        {
            currentTarget = portal;
            arrowImage.gameObject.SetActive(true);
        }
        else
        {
            arrowImage.gameObject.SetActive(false);
            Debug.Log($"当前场景 {currentScene} 没有通往 {nextScene} 的传送门");
        }
    }

    /// <summary>
    /// 在当前场景中查找属于该任务ID的任务触发器 (Plot 类型)
    /// </summary>
    private Transform FindQuestTriggerInScene(string questId)
    {
        QuestTriggerZone[] zones = FindObjectsOfType<QuestTriggerZone>();
        foreach (var zone in zones)
        {
            if (zone.triggerType == QuestTriggerZone.TriggerType.Plot && zone.questId == questId)
                return zone.transform;
        }

        CombatQuestTrigger[] combatZones = FindObjectsOfType<CombatQuestTrigger>();
        foreach (var zone in combatZones)
        {
            if (zone.questId == questId)
                return zone.transform;
        }
        return null;
    }
    private void Update()
    {
        if (currentTarget == null || playerTransform == null)
            return;

        Vector3 dir3 = currentTarget.position - playerTransform.position;
        dir3.z = 0;
        if (dir3 == Vector3.zero) return;

        Vector2 direction = new Vector2(dir3.x, dir3.y).normalized;
        float angleRad = Mathf.Atan2(direction.y, direction.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        Vector2 circleOffset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 targetScreenPos = screenCenter + circleOffset;
        targetScreenPos.x = Mathf.Clamp(targetScreenPos.x, 0, Screen.width);
        targetScreenPos.y = Mathf.Clamp(targetScreenPos.y, 0, Screen.height);

        Vector2 localPos = targetScreenPos - screenCenter;
        arrowImage.anchoredPosition = localPos;

        // 如果箭头指向传送门，保持指向；如果指向任务触发器，同样适用
        arrowImage.rotation = Quaternion.Euler(0, 0, angleDeg + 90f);
    }

    /// <summary>
    /// 在当前场景中查找通往目标场景的传送门 (仅限已解锁的，即前置任务已完成)
    /// </summary>
    private Transform FindPortalToScene(string targetScene)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        QuestTriggerZone[] zones = FindObjectsOfType<QuestTriggerZone>();
        Transform best = null;
        float minDist = float.MaxValue;

        foreach (var zone in zones)
        {
            if (zone.triggerType == QuestTriggerZone.TriggerType.Scene && zone.targetSceneName == targetScene)
            {
                // 关键：检查该传送门是否已经解锁（前置任务完成）
                bool isUnlocked = PlayerDataManager.Instance.HasCompletedQuest(zone.questId);
                if (!isUnlocked) continue;  // 未解锁的传送门不可用，跳过

                float dist = Vector3.Distance(playerTransform.position, zone.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = zone.transform;
                }
            }
        }
        return best;
    }
}