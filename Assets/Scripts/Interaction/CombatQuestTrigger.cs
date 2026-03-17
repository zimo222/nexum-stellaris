using UnityEngine;

public class CombatQuestTrigger : MonoBehaviour
{
    [Header("任务设置")]
    public string questId;               // 要触发的战斗任务ID
    public Transform spawnCenter;         // 敌人生成中心点（通常就是触发器本身位置，也可指定）

    private bool playerInZone = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            // 可选：显示提示UI "按F开始战斗"
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            // 隐藏提示
        }
    }

    private void Update()
    {
        if (playerInZone && Input.GetKeyDown(KeyCode.F))
        {
            TryStartCombat();
        }
    }

    private void TryStartCombat()
    {
        // 检查任务是否已完成或正在进行
        var questProgress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (questProgress != null)
        {
            // 任务已在进行中，不允许重复触发
            Debug.Log("任务已在进行中");
            return;
        }

        if (PlayerDataManager.Instance.HasCompletedQuest(questId))
        {
            Debug.Log("任务已完成，无法再次触发");
            return;
        }

        // 调用任务管理器开始战斗任务
        QuestManager.Instance.StartCombatQuest(questId, spawnCenter.position);
    }
}