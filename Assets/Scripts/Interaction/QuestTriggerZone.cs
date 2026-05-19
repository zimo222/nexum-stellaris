using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class QuestTriggerZone : MonoBehaviour
{
    public enum TriggerType { Plot, Scene }
    public TriggerType triggerType;
    public string questId;
    public string targetSceneName;
    public int xposition, yposition;
    public GameObject trackingIndicator;
    public GameObject interactButton;

    [Tooltip("按钮的名称（必须完全匹配，区分大小写）")]
    public string buttonName = "InterationButton";

    [Tooltip("最大重试次数")]
    public int maxRetryCount = 10;

    [Tooltip("每次重试间隔（秒）")]
    public float retryInterval = 0.2f;

    private bool playerInZone;
    private bool taskStarted;
    private bool isHotReferencingDone = false;
    private Coroutine findButtonCoroutine;
    private bool isRegistered = false;

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
        if (QuestManager.Instance != null)
            OnTrackedQuestChanged(QuestManager.Instance.TrackedQuestId);
        if (interactButton == null && !isHotReferencingDone && !string.IsNullOrEmpty(buttonName))
        {
            if (findButtonCoroutine != null) StopCoroutine(findButtonCoroutine);
            findButtonCoroutine = StartCoroutine(DelayedHotReferenceWithRetry());
        }
        else if (interactButton != null)
        {
            ConfigureButton();
        }
        RegisterWithSceneGraph();
    }

    private void Update()
    {
        // Scene 类型按 F 直接交互（按钮也会显示，但 F 键更快捷）
        if (triggerType == TriggerType.Scene && playerInZone && !taskStarted && Input.GetKeyDown(KeyCode.F))
        {
            if (PlayerDataManager.Instance.HasCompletedQuest(questId))
            {
                taskStarted = true;
                if (IsReferenceValid(interactButton)) interactButton.SetActive(false);
                LoadTargetScene();
            }
            else
            {
                PromptTextManager.Instance.ShowMessage("前面的区域，以后再探索吧！\n(完成主线剧情   第" + questId[12] + "章第" + questId[15] + "幕  " + GameDataManager.Instance.QuestDict[questId].questName + "   解锁)");
            }
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
                if (interactButton != null)
                {
                    Debug.Log($"成功热引用按钮（第{retry + 1}次尝试）：{interactButton.name}");
                    break;
                }
            }
            retry++;
        }
        if (IsReferenceValid(interactButton))
            ConfigureButton();
        else
            Debug.LogWarning("QuestTriggerZone: 无法获得交互按钮引用，将无法触发交互");
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
            btn.onClick.RemoveListener(OnInteractButtonClicked);
            btn.onClick.AddListener(OnInteractButtonClicked);
        }
        else
        {
            Debug.LogError("interactButton 上缺少 Button 组件");
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;
        if (IsReferenceValid(interactButton))
        {
            Button btn = interactButton.GetComponent<Button>();
            if (btn != null) btn.onClick.RemoveListener(OnInteractButtonClicked);
        }
        if (findButtonCoroutine != null) StopCoroutine(findButtonCoroutine);
        UnregisterFromSceneGraph();
    }

    private GameObject[] GetAllRootGameObjects()
    {
        var activeSceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
        Scene ddolScene = SceneManager.GetSceneByName("DontDestroyOnLoad");
        if (ddolScene.isLoaded)
        {
            var ddolRoots = ddolScene.GetRootGameObjects();
            var combined = new GameObject[activeSceneRoots.Length + ddolRoots.Length];
            activeSceneRoots.CopyTo(combined, 0);
            ddolRoots.CopyTo(combined, activeSceneRoots.Length);
            return combined;
        }
        return activeSceneRoots;
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
        {
            if (triggerType == TriggerType.Plot)
                trackingIndicator.SetActive(trackedQuestId == questId && !QuestManager.Instance.isDialoguePlaying);
            else
                trackingIndicator.SetActive(true);
        }
    }

    public static void RefreshGlobalButton()
    {
        if (QuestManager.Instance == null) return;
        bool shouldShow = false;

        // 1. 检查剧情触发器 (Plot 和 Scene)
        foreach (var zone in FindObjectsOfType<QuestTriggerZone>())
        {
            if (!zone.playerInZone) continue;
            if (zone.triggerType == TriggerType.Plot && zone.IsQuestAvailable())
            {
                shouldShow = true;
                break;
            }
            if (zone.triggerType == TriggerType.Scene)
            {
                shouldShow = true;
                break;
            }
        }

        // 2. 如果没有显示，再检查战斗触发器
        if (!shouldShow)
        {
            foreach (var combatZone in FindObjectsOfType<CombatQuestTrigger>())
            {
                if (combatZone.IsPlayerInZone() && combatZone.IsQuestAvailable())
                {
                    shouldShow = true;
                    break;
                }
            }
        }

        if (QuestManager.Instance.interationButton != null && QuestManager.Instance.interationButton.activeSelf != shouldShow)
            QuestManager.Instance.interationButton.SetActive(shouldShow);
    }

    private bool IsQuestAvailable()
    {
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null) return false;
        if (progress.state != QuestProgressState.Available) return false;
        if (PlayerDataManager.Instance.HasCompletedQuest(questId)) return false;
        return true;
    }

    private void OnInteractButtonClicked()
    {
        if (!playerInZone) return;
        if (taskStarted) return;
        if (triggerType == TriggerType.Plot)
        {
            if (!IsQuestAvailable()) return;
            taskStarted = true;
            if (IsReferenceValid(interactButton)) interactButton.SetActive(false);
            QuestManager.Instance?.StartCurrentQuest();
        }
        else if (triggerType == TriggerType.Scene)
        {
            if (PlayerDataManager.Instance.HasCompletedQuest(questId))
            {
                taskStarted = true;
                if (IsReferenceValid(interactButton)) interactButton.SetActive(false);
                LoadTargetScene();
            }
            else
            {
                PromptTextManager.Instance.ShowMessage("前面的区域，以后再探索吧！\n(完成主线剧情   第" + questId[12] + "章第" + questId[15] + "幕  " + GameDataManager.Instance.QuestDict[questId].questName + "   解锁)");
            }
        }
    }

    private void LoadTargetScene()
    {
        if (SceneDataManager.Instance != null)
            SceneDataManager.Instance.LoadScene(targetSceneName, xposition, yposition);
        else
            Debug.LogError("SceneDataManager.Instance 为空，无法切换场景");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (QuestManager.Instance.isDialoguePlaying) return;

        if (triggerType == TriggerType.Plot)
        {
            playerInZone = true;
            taskStarted = false;
            QuestManager.Instance?.OnPlayerEnterQuestArea(questId);
        }
        else if (triggerType == TriggerType.Scene)
        {
            playerInZone = true;
            taskStarted = false;
        }
        RefreshGlobalButton();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (triggerType == TriggerType.Scene)
        {
            playerInZone = false;
            if (IsReferenceValid(interactButton)) interactButton.SetActive(false);
            taskStarted = false;
        }
        else if (triggerType == TriggerType.Plot)
        {
            playerInZone = false;
            taskStarted = false;
            if (IsReferenceValid(interactButton)) interactButton.SetActive(false);
            QuestManager.Instance?.OnPlayerExitQuestArea(questId);
        }
        RefreshGlobalButton();
    }

    public void DisableButton()
    {
        if (IsReferenceValid(interactButton))
            interactButton.SetActive(false);
        taskStarted = true;
    }

    public void RefreshButtonReference()
    {
        if (findButtonCoroutine != null) StopCoroutine(findButtonCoroutine);
        interactButton = null;
        isHotReferencingDone = false;
        findButtonCoroutine = StartCoroutine(DelayedHotReferenceWithRetry());
    }

    private void RegisterWithSceneGraph()
    {
        if (triggerType == TriggerType.Scene && SceneGraphManager.Instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            SceneGraphManager.Instance.RegisterPortal(currentScene, targetSceneName, transform.position, questId);
            isRegistered = true;
        }
    }

    private void UnregisterFromSceneGraph()
    {
        if (triggerType == TriggerType.Scene && SceneGraphManager.Instance != null && isRegistered)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            SceneGraphManager.Instance.UnregisterPortal(currentScene, transform.position);
            isRegistered = false;
        }
    }
}