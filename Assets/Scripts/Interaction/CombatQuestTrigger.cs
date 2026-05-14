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
    private bool taskStarted = false;  // 任务是否已经开始
    private bool isHotReferencingDone = false;
    private Coroutine findButtonCoroutine;  // 新增：用于管理协程

    // 在类的成员变量区域添加：
    private bool isRegistered = false;

    // 新增：检测 Unity 对象是否真实有效（不被销毁）
    private bool IsReferenceValid(Object obj)
    {
        return (object)obj != null && obj != null;
    }

    private void OnEnable()
    {
        TrySubscribe();

        // 关键修复：检查现有引用是否真实有效，无效则置空并重置标记
        if (!IsReferenceValid(interactButton))
        {
            interactButton = null;
            isHotReferencingDone = false;
        }

        // 如果按钮无效且未完成热引用，启动查找协程
        if (interactButton == null && !isHotReferencingDone && !string.IsNullOrEmpty(buttonName))
        {
            if (findButtonCoroutine != null)
                StopCoroutine(findButtonCoroutine);
            findButtonCoroutine = StartCoroutine(DelayedHotReferenceWithRetry());
        }
        else if (interactButton != null)
        {
            ConfigureButton();
        }
    }

    // 其余方法保持不变（FindInactiveGameObjectByName, GetGameObjectPath, GetAllRootNames, ConfigureButton, OnDestroy, 等）
    // 注意：在 OnDestroy 和 ConfigureButton 中也建议使用 IsReferenceValid 检查，但原代码大部分已使用 null 检查，
    // 为了统一，我将原代码中的 interactButton != null 替换为 IsReferenceValid(interactButton)

    // 以下是原代码中未修改的方法，但为了保险，将 null 检查改为 IsReferenceValid
    private GameObject FindInactiveGameObjectByName(string name)
    {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.scene == null || !obj.scene.IsValid()) continue;
            if (obj.name == name)
                return obj;
        }
        return null;
    }

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

    private string[] GetAllRootNames()
    {
        var roots = GetAllRootGameObjects();
        string[] names = new string[roots.Length];
        for (int i = 0; i < roots.Length; i++)
            names[i] = roots[i].name;
        return names;
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

    private void ConfigureButton()
    {
        if (!IsReferenceValid(interactButton)) return;

        interactButton.SetActive(false);
        Button btn = interactButton.GetComponent<Button>();
        if (btn != null)
        {
            /*
            btn.onClick.RemoveListener(OnInteractButtonClicked);
            btn.onClick.AddListener(OnInteractButtonClicked);
            */
        }
        else
        {
            Debug.LogError("interactButton 上缺少 Button 组件");
        }
    }

    private IEnumerator DelayedHotReferenceWithRetry()
    {
        int retry = 0;
        while (retry < maxRetryCount)
        {
            yield return new WaitForSeconds(retryInterval);

            // 同样使用有效性检查
            if (!IsReferenceValid(interactButton) && !string.IsNullOrEmpty(buttonName))
            {
                interactButton = FindInactiveGameObjectByName(buttonName);
                if (interactButton != null)
                {
                    Debug.Log($"{this.gameObject.name}成功热引用按钮（第{retry + 1}次尝试）：{interactButton.name}，路径：{GetGameObjectPath(interactButton)}");
                    break;
                }
                else
                {
                    if (retry == 0)
                        Debug.Log($"未找到名为 '{buttonName}' 的按钮。当前场景所有根物体：{string.Join(", ", GetAllRootNames())}");
                    else
                        Debug.Log($"未找到按钮（第{retry + 1}/{maxRetryCount}次尝试）...");
                }
            }
            retry++;
        }

        if (!IsReferenceValid(interactButton))
        {
            Debug.LogError($"经过 {maxRetryCount} 次尝试后仍未找到名称为 '{buttonName}' 的按钮。请检查：\n" +
                           "1. 按钮的实际名称（包括大小写、空格）\n" +
                           "2. 按钮是否在场景中（即使是未激活状态）\n" +
                           "3. 尝试在 Inspector 中手动拖拽引用");
        }

        if (IsReferenceValid(interactButton))
            ConfigureButton();
        else
            Debug.LogError("QuestTriggerZone: 无法获得交互按钮引用，将无法触发交互");

        isHotReferencingDone = true;
        findButtonCoroutine = null;
    }

    private void Start()
    {
        TrySubscribe();

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

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;

        if (interactButton != null)
        {
            Button btn = interactButton.GetComponent<Button>();
            if (btn != null)
                btn.onClick.RemoveListener(TryStartCombat);
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

    // 在 QuestTriggerZone 类中添加这个公共方法
    public void RefreshButtonReference()
    {
        // 停止正在进行的查找协程（如果有）
        if (findButtonCoroutine != null)
            StopCoroutine(findButtonCoroutine);

        // 重置状态，强制重新查找
        interactButton = null;
        isHotReferencingDone = false;

        // 启动查找协程
        findButtonCoroutine = StartCoroutine(DelayedHotReferenceWithRetry());
    }
}