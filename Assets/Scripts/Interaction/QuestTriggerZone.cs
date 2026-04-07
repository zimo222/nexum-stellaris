using UnityEngine;
using UnityEngine.UI;

public class QuestTriggerZone : MonoBehaviour
{
    public enum TriggerType { Plot, Scene }
    public TriggerType triggerType;
    public string questId;
    public string targetSceneName;
    public GameObject trackingIndicator;
    public GameObject interactButton;

    private bool playerInZone;
    private bool taskStarted = false;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        if (QuestManager.Instance != null)
            OnTrackedQuestChanged(QuestManager.Instance.TrackedQuestId);

        if (triggerType == TriggerType.Plot && interactButton != null)
        {
            interactButton.SetActive(false);
            Button btn = interactButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(TryStartDialogue);
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;
    }

    private void TrySubscribe()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;
            QuestManager.Instance.OnTrackedQuestChanged += OnTrackedQuestChanged;
            OnTrackedQuestChanged(QuestManager.Instance.TrackedQuestId);
        }
    }

    private void OnTrackedQuestChanged(string trackedQuestId)
    {
        if (trackingIndicator != null)
            trackingIndicator.SetActive(trackedQuestId == questId);
    }

    private void Update()
    {
        if (triggerType == TriggerType.Plot && !taskStarted && playerInZone && interactButton != null)
        {
            bool shouldShow = IsQuestAvailable();
            if (interactButton.activeSelf != shouldShow)
                interactButton.SetActive(shouldShow);
        }

        if (triggerType == TriggerType.Scene && playerInZone && Input.GetKeyDown(KeyCode.F))
        {
            SceneDataManager.Instance.LoadScene(targetSceneName);
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
        if (!other.CompareTag("Player")) return;

        if (triggerType == TriggerType.Plot)
        {
            playerInZone = true;
            taskStarted = false;
            QuestManager.Instance?.OnPlayerEnterQuestArea(questId);
        }
        else if (triggerType == TriggerType.Scene)
        {
            playerInZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (triggerType == TriggerType.Scene)
        {
            playerInZone = false;
        }
        else if (triggerType == TriggerType.Plot)
        {
            playerInZone = false;
            taskStarted = false;
            if (interactButton != null) interactButton.SetActive(false);
            QuestManager.Instance?.OnPlayerExitQuestArea(questId);
        }
    }

    private void TryStartDialogue()
    {
        if (taskStarted) return;
        if (!IsQuestAvailable()) return;

        taskStarted = true;
        if (interactButton != null) interactButton.SetActive(false);

        QuestManager.Instance?.StartCurrentQuest();
    }

    // ========== 新增方法：供 QuestManager 按F时调用 ==========
    public void DisableButton()
    {
        if (interactButton != null)
            interactButton.SetActive(false);
        taskStarted = true;
    }
}