using UnityEngine;

public class QuestTriggerZone : MonoBehaviour
{
    public enum TriggerType { Plot, Scene }
    public TriggerType triggerType;
    public string questId;
    public string targetSceneName;
    public GameObject trackingIndicator;

    private bool playerInZone;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        if (QuestManager.Instance != null)
            OnTrackedQuestChanged(QuestManager.Instance.TrackedQuestId);
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
        if (triggerType == TriggerType.Scene && playerInZone && Input.GetKeyDown(KeyCode.F))
        {
            SceneDataManager.Instance.LoadScene(targetSceneName);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

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
        if (!other.CompareTag("Player")) return;

        if (triggerType == TriggerType.Scene)
            playerInZone = false;
    }
}