using UnityEngine;

public class QuestTriggerZone : MonoBehaviour
{
    // 触发器类型枚举
    public enum TriggerType
    {
        Plot,   // 剧情触发：玩家进入区域时通知任务管理器
        Scene   // 场景触发：玩家在区域内按 F 键切换场景
    }

    [Header("基础设置")]
    public TriggerType triggerType;     // 当前触发器的类型
    public string questId;              // 任务 ID（剧情类型使用）

    [Header("场景切换设置（仅 Scene 类型有效）")]
    public string targetSceneName;      // 目标场景名称，例如 "1_TheNestOfWarmLight_0"

    private bool playerInZone;          // 玩家是否在触发器区域内

    private void Update()
    {
        // 仅当类型为 Scene 且玩家在区域内时检测 F 键
        if (triggerType == TriggerType.Scene && playerInZone)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                // 调用场景管理器加载目标场景
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
            // 剧情类型：通知任务管理器
            QuestManager.Instance?.OnPlayerEnterQuestArea(questId);
        }
        else if (triggerType == TriggerType.Scene)
        {
            // 场景类型：标记玩家进入区域
            playerInZone = true;
            // 可选：在这里显示提示 UI（例如“按 F 进入下一场景”）
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (triggerType == TriggerType.Scene)
        {
            // 玩家离开区域，清除标记
            playerInZone = false;
            // 可选：隐藏提示 UI
        }
    }
}