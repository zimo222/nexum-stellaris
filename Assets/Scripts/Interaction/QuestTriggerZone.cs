using UnityEngine;

public class QuestTriggerZone : MonoBehaviour
{
    public enum TriggerType
    {
        Plot,
        Scene
    }

    [Header("基础设置")]
    public TriggerType triggerType;
    public string questId;

    [Header("场景切换设置（仅 Scene 类型有效）")]
    public string targetSceneName;

    [Header("追踪指示器")]
    public GameObject trackingIndicator;

    private bool playerInZone;

    private void OnEnable()
    {
        // 每次激活时尝试订阅
        TrySubscribe();
    }

    private void Start()
    {
        // 再次尝试订阅（确保即使 OnEnable 时 Instance 为 null，也能在 Start 时成功）
        TrySubscribe();
        // 同时更新指示器状态
        if (QuestManager.Instance != null)
        {
            OnTrackedQuestChanged(QuestManager.Instance.TrackedQuestId);
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;
        }
    }

    private void TrySubscribe()
    {
        if (QuestManager.Instance != null)
        {
            // 避免重复订阅（先取消再添加）
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;
            QuestManager.Instance.OnTrackedQuestChanged += OnTrackedQuestChanged;
            // 立即更新状态
            OnTrackedQuestChanged(QuestManager.Instance.TrackedQuestId);
        }
        // 如果 Instance 为 null，则等 Start 再试
    }

    private void OnTrackedQuestChanged(string trackedQuestId)
    {
        if (trackingIndicator != null)
        {
            bool shouldShow = (trackedQuestId == questId);
            trackingIndicator.SetActive(shouldShow);
        }
        else
        {
            Debug.LogError($"[触发器 {questId}] trackingIndicator 未赋值！");
        }
    }

    private void Update()
    {
        if (triggerType == TriggerType.Scene && playerInZone)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                SceneDataManager.Instance.LoadScene(targetSceneName);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (triggerType == TriggerType.Plot)
        {
            QuestManager.Instance?.OnPlayerEnterQuestArea(questId);
        }
        else if (triggerType == TriggerType.Scene)
        {
            playerInZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (triggerType == TriggerType.Scene)
        {
            playerInZone = false;
        }
    }
}