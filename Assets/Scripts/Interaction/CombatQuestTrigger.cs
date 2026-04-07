using UnityEngine;
using UnityEngine.UI;

public class CombatQuestTrigger : MonoBehaviour
{
    public string questId;
    public Transform spawnCenter;
    public GameObject interactButton;

    private bool playerInZone = false;
    private bool taskStarted = false;  // 任务是否已经开始

    private void Start()
    {
        if (interactButton != null)
        {
            interactButton.SetActive(false);
            Button btn = interactButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(TryStartCombat);
        }
    }

    private void Update()
    {
        // 只有在未开始任务且区域内时，才根据任务状态控制按钮显示
        if (!taskStarted && playerInZone && interactButton != null)
        {
            bool shouldShow = IsQuestAvailable();
            if (interactButton.activeSelf != shouldShow)
                interactButton.SetActive(shouldShow);
        }

        if (playerInZone && Input.GetKeyDown(KeyCode.F) && !taskStarted)
        {
            TryStartCombat();
        }
    }

    private bool IsQuestAvailable()
    {
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null) return false;
        if (progress.state != QuestProgressState.Available) return false;
        if (PlayerDataManager.Instance.HasCompletedQuest(questId)) return false;
        return true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            taskStarted = false;  // 重新进入时重置标志
            QuestManager.Instance?.OnPlayerEnterQuestArea(questId, spawnCenter.position);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            taskStarted = false;
            if (interactButton != null) interactButton.SetActive(false);
            QuestManager.Instance?.OnPlayerExitQuestArea(questId);
        }
    }

    private void TryStartCombat()
    {
        if (taskStarted) return;
        if (!IsQuestAvailable()) return;

        taskStarted = true;
        // 开始战斗前立即隐藏按钮
        if (interactButton != null) interactButton.SetActive(false);

        QuestManager.Instance.StartCombatQuest(questId, spawnCenter.position);
    }
}