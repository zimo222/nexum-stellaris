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
    public GameObject trackingIndicator;
    public GameObject interactButton;

    [Tooltip("按钮的名称（必须完全匹配，区分大小写）")]
    public string buttonName = "InterationButton";

    [Tooltip("最大重试次数")]
    public int maxRetryCount = 10;

    [Tooltip("每次重试间隔（秒）")]
    public float retryInterval = 0.2f;

    private bool playerInZone;
    private bool taskStarted = false;
    private bool isHotReferencingDone = false;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        if (QuestManager.Instance != null)
            OnTrackedQuestChanged(QuestManager.Instance.TrackedQuestId);

        if (triggerType == TriggerType.Plot)
        {
            if (interactButton == null && !isHotReferencingDone)
            {
                StartCoroutine(DelayedHotReferenceWithRetry());
            }
            else if (interactButton != null)
            {
                ConfigureButton();
            }
        }
    }

    private IEnumerator DelayedHotReferenceWithRetry()
    {
        int retry = 0;
        while (retry < maxRetryCount)
        {
            yield return new WaitForSeconds(retryInterval);

            if (interactButton == null && !string.IsNullOrEmpty(buttonName))
            {
                interactButton = FindInactiveGameObjectByName(buttonName);
                if (interactButton != null)
                {
                    Debug.Log($"成功热引用按钮（第{retry + 1}次尝试）：{interactButton.name}，路径：{GetGameObjectPath(interactButton)}");
                    break;
                }
                else
                {
                    if (retry == 0) // 只在第一次失败时输出所有根物体信息，避免刷屏
                        Debug.Log($"未找到名为 '{buttonName}' 的按钮。当前场景所有根物体：{string.Join(", ", GetAllRootNames())}");
                    else
                        Debug.Log($"未找到按钮（第{retry + 1}/{maxRetryCount}次尝试）...");
                }
            }
            retry++;
        }

        if (interactButton == null)
        {
            Debug.LogError($"经过 {maxRetryCount} 次尝试后仍未找到名称为 '{buttonName}' 的按钮。请检查：\n" +
                           "1. 按钮的实际名称（包括大小写、空格）\n" +
                           "2. 按钮是否在场景中（即使是未激活状态）\n" +
                           "3. 尝试在 Inspector 中手动拖拽引用");
        }

        if (interactButton != null)
            ConfigureButton();
        else
            Debug.LogError("QuestTriggerZone: 无法获得交互按钮引用，将无法触发对话");

        isHotReferencingDone = true;
    }

    // 使用 Resources.FindObjectsOfTypeAll 查找未激活的 GameObject
    private GameObject FindInactiveGameObjectByName(string name)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            // 过滤掉预制体、资源文件等（只保留场景中的实例）
            if (obj.scene == null || !obj.scene.IsValid()) continue;
            if (obj.name == name)
            {
                return obj;
            }
        }
        return null;
    }

    // 辅助方法：获取 GameObject 的完整路径（用于调试）
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    // 获取当前所有根物体的名称（用于调试）
    private string[] GetAllRootNames()
    {
        var roots = GetAllRootGameObjects();
        string[] names = new string[roots.Length];
        for (int i = 0; i < roots.Length; i++)
            names[i] = roots[i].name;
        return names;
    }

    private void ConfigureButton()
    {
        if (interactButton == null) return;

        interactButton.SetActive(false);
        Button btn = interactButton.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(TryStartDialogue);
            btn.onClick.AddListener(TryStartDialogue);
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

        if (interactButton != null)
        {
            Button btn = interactButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveListener(TryStartDialogue);
        }
    }

    // ========== 获取所有根物体（包括 DontDestroyOnLoad） ==========
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

    // ========== 其余原有方法（保持不变） ==========
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

    public void DisableButton()
    {
        if (interactButton != null)
            interactButton.SetActive(false);
        taskStarted = true;
    }
}