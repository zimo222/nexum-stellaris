using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class CombatQuestTrigger : MonoBehaviour
{
    public string questId;
    public GameObject trackingIndicator;
    public Transform spawnCenter;
    public GameObject interactButton;

    [Tooltip("按钮的名称（必须完全匹配，区分大小写）")]
    public string buttonName = "InterationButton";

    [Tooltip("最大重试次数")]
    public int maxRetryCount = 10;

    [Tooltip("每次重试间隔（秒）")]
    public float retryInterval = 0.2f;

    private bool playerInZone = false;
    private bool taskStarted = false;
    private bool isHotReferencingDone = false;
    private Coroutine findButtonCoroutine;

    private bool IsReferenceValid(Object obj) => (object)obj != null && obj != null;

    private void OnEnable()
    {
        TrySubscribe();
        if (!IsReferenceValid(interactButton))
        {
            interactButton = null;
            isHotReferencingDone = false;
        }
        if (interactButton == null && !isHotReferencingDone && !string.IsNullOrEmpty(buttonName))
        {
            if (findButtonCoroutine != null) StopCoroutine(findButtonCoroutine);
            findButtonCoroutine = StartCoroutine(DelayedHotReferenceWithRetry());
        }
        else if (interactButton != null)
        {
            ConfigureButton();
        }
    }

    private void Start()
    {
        TrySubscribe();
        if (interactButton != null)
        {
            interactButton.SetActive(false);
            Button btn = interactButton.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(TryStartCombat);
        }
    }

    private void Update()
    {
        // 监听 F 键
        if (playerInZone && !taskStarted && Input.GetKeyDown(KeyCode.F))
        {
            TryStartCombat();
        }
    }

    private IEnumerator DelayedHotReferenceWithRetry()
    {
        int retry = 0;
        while (retry < maxRetryCount)
        {
            yield return new WaitForSeconds(retryInterval);
            if (!IsReferenceValid(interactButton) && !string.IsNullOrEmpty(buttonName))
            {
                interactButton = FindInactiveGameObjectByName(buttonName);
                if (interactButton != null) break;
            }
            retry++;
        }
        if (IsReferenceValid(interactButton))
            ConfigureButton();
        else
            Debug.LogError($"CombatQuestTrigger: 无法获得交互按钮引用，将无法触发交互");
        isHotReferencingDone = true;
        findButtonCoroutine = null;
    }

    private GameObject FindInactiveGameObjectByName(string name)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
            if (obj.scene.IsValid() && obj.name == name) return obj;
        return null;
    }

    private void ConfigureButton()
    {
        if (!IsReferenceValid(interactButton)) return;
        interactButton.SetActive(false);
        Button btn = interactButton.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(TryStartCombat);
            btn.onClick.AddListener(TryStartCombat);
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null) QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;
        if (interactButton != null)
        {
            Button btn = interactButton.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(TryStartCombat);
        }
        if (findButtonCoroutine != null) StopCoroutine(findButtonCoroutine);
    }

    public bool IsQuestAvailable()
    {
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null) return false;
        if (progress.state != QuestProgressState.Available) return false;
        if (PlayerDataManager.Instance.HasCompletedQuest(questId)) return false;
        return true;
    }

    public bool IsPlayerInZone() => playerInZone;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            taskStarted = false;
            QuestManager.Instance?.OnPlayerEnterQuestArea(questId, spawnCenter.position);
            QuestTriggerZone.RefreshGlobalButton(); // 统一刷新全局按钮
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
            QuestTriggerZone.RefreshGlobalButton();
        }
    }

    private void TryStartCombat()
    {
        if (taskStarted || !IsQuestAvailable()) return;
        taskStarted = true;
        if (interactButton != null) interactButton.SetActive(false);
        QuestManager.Instance.StartCombatQuest(questId, spawnCenter.position);
        QuestTriggerZone.RefreshGlobalButton();
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

    public void RefreshButtonReference()
    {
        if (findButtonCoroutine != null) StopCoroutine(findButtonCoroutine);
        interactButton = null;
        isHotReferencingDone = false;
        findButtonCoroutine = StartCoroutine(DelayedHotReferenceWithRetry());
    }
}