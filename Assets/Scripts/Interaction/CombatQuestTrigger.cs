using UnityEngine;

public class CombatQuestTrigger : MonoBehaviour
{
    public string questId;
    public Transform spawnCenter;

    private bool playerInZone = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            // 可显示 UI 提示
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
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
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null || progress.state != QuestProgressState.Available)
        {
            Debug.Log($"任务 {questId} 状态不是 Available，无法开始战斗");
            return;
        }

        if (PlayerDataManager.Instance.HasCompletedQuest(questId))
        {
            Debug.Log("任务已完成");
            return;
        }

        QuestManager.Instance.StartCombatQuest(questId, spawnCenter.position);
    }
}