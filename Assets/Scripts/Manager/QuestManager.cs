using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("常驻UI")]
    public TMP_Text guestText;

    [Header("弹出面板")]
    public GameObject questPanel;
    public TMP_Text panelTaskNameText;
    public TMP_Text panelStatusText;

    [Header("对话UI")]
    public GameObject dialoguePanel;
    public Image backgroundImage;
    public TMP_Text speakerText;
    public TMP_Text dialogueContentText;

    [Header("动画参数")]
    public float fadeDuration = 0.3f;
    public float displayTime = 1f;
    public bool enablePanelAnimation = true;

    [Header("打字效果")]
    public float typingSpeed = 0.05f;

    private CanvasGroup panelCanvasGroup;
    private Coroutine panelCoroutine;
    private List<DialogueEntry> currentDialogue;
    private int currentDialogueIndex;
    private bool isDialoguePlaying;
    private Coroutine typingCoroutine;
    private bool isTextFullyDisplayed;
    private Player playerController;
    private string currentInteractiveQuestId;
    private bool waitingForInteraction = false;
    private bool isQuestActive = false;        // 标记当前是否处于启动状态（对话或战斗）

    // 任务追踪相关
    public string TrackedQuestId { get; private set; }
    public event System.Action<string> OnTrackedQuestChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (questPanel != null)
        {
            panelCanvasGroup = questPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = questPanel.AddComponent<CanvasGroup>();
            questPanel.SetActive(false);
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);
    }

    public void FindPlayer()
    {
        GameObject[] players = null;
        while (playerController == null)
        {
            players = GameObject.FindGameObjectsWithTag("Player");
            if (players.Length > 0)
                playerController = players[0].GetComponent<Player>();
        }
    }

    void Start()
    {
        FindPlayer();

        // 修复旧存档数据：将 NotStarted/InProgress 统一转换为 Available
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData != null)
        {
            bool dataChanged = false;
            foreach (var progress in playerData.activeQuests)
            {
                // 旧枚举值转换（假设旧值为 QuestState.NotStarted 或 QuestState.InProgress）
                // 为了兼容，我们通过反射或直接字段判断，这里简化：如果 state 不是 Available 也不是 Completed，就设为 Available
                if (progress.state != QuestProgressState.Available && progress.state != QuestProgressState.Completed)
                {
                    progress.state = QuestProgressState.Available;
                    dataChanged = true;
                    Debug.Log($"修复任务状态: {progress.questId} -> Available");
                }

                // 确保战斗任务的目标列表完整
                if (GameDataManager.Instance.QuestDict.TryGetValue(progress.questId, out var questData) &&
                    questData.contentType == QuestContentType.Combat &&
                    questData.objectives != null)
                {
                    if (progress.objectives.Count != questData.objectives.Count)
                    {
                        progress.objectives.Clear();
                        foreach (var objDefine in questData.objectives)
                        {
                            progress.objectives.Add(new ObjectiveProgress(objDefine.objectiveId, 0, objDefine.requiredAmount, false));
                        }
                        dataChanged = true;
                    }
                }
            }
            if (dataChanged)
            {
                PlayerDataManager.Instance.SaveCurrentPlayerData();
            }
        }

        RefreshQuestUI();
        AutoSetTrackedQuest();
    }

    void Update()
    {
        // 对话空格处理（启动状态下的交互）
        if (isDialoguePlaying && Input.GetKeyDown(KeyCode.Space))
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                dialogueContentText.text = currentDialogue[currentDialogueIndex].content;
                isTextFullyDisplayed = true;
            }
            else if (isTextFullyDisplayed)
            {
                currentDialogueIndex++;
                if (currentDialogueIndex < currentDialogue.Count)
                {
                    ShowDialogueEntry(currentDialogue[currentDialogueIndex]);
                }
                else
                {
                    EndDialogue();
                }
            }
        }

        // 等待玩家按F键开始任务（进入启动状态）
        if (waitingForInteraction && Input.GetKeyDown(KeyCode.F))
        {
            if (playerController != null && !playerController.isIdle)
            {
                Debug.Log("移动中不能开始任务");
                return;
            }

            if (!string.IsNullOrEmpty(currentInteractiveQuestId))
            {
                var progress = PlayerDataManager.Instance?.GetQuestProgress(currentInteractiveQuestId);
                if (progress != null && progress.state == QuestProgressState.Available &&
                    GameDataManager.Instance.QuestDict.TryGetValue(currentInteractiveQuestId, out var questData))
                {
                    if (questData.contentType == QuestContentType.Dialogue)
                    {
                        waitingForInteraction = false;
                        isQuestActive = true;   // 标记启动状态
                        StartDialogue(questData.dialogueEntries, currentInteractiveQuestId);
                    }
                    else if (questData.contentType == QuestContentType.Combat)
                    {
                        waitingForInteraction = false;
                        isQuestActive = true;
                        // 启动战斗，由 CombatQuestTrigger 调用 StartCombatQuest，此处不再重复
                        // 注意：StartCombatQuest 中会调用 CombatManager.StartCombat，需要将任务标记为启动
                        // 我们会在 StartCombatQuest 中设置 isQuestActive
                    }
                }
            }
        }
    }

    /// <summary>
    /// 解锁任务（前置任务完成后调用）
    /// </summary>
    public void UnlockQuest(string questId)
    {
        if (PlayerDataManager.Instance.UnlockQuest(questId))
        {
            RefreshQuestUI();
            AutoSetTrackedQuest();
            // 可选：显示提示 "新任务已解锁"
        }
    }

    /// <summary>
    /// 完成一个任务，并自动解锁后续任务
    /// </summary>
    public void CompleteQuest(string questId)
    {
        if (PlayerDataManager.Instance.CompleteQuest(questId))
        {
            if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData))
            {
                ShowPanel(questData.questName, "完成");

                // 解锁后续任务
                if (questData.nextQuestIds != null)
                {
                    foreach (string nextId in questData.nextQuestIds)
                    {
                        if (!PlayerDataManager.Instance.HasCompletedQuest(nextId))
                        {
                            UnlockQuest(nextId);
                        }
                    }
                }

                // 发放奖励
                if (questData.Reward != null)
                {
                    foreach (string rewardId in questData.Reward)
                    {
                        if (rewardId[0] == 'E')
                            PlayerDataManager.Instance.AddExotext(rewardId);
                        else
                            PlayerDataManager.Instance.AddNexusVesture(rewardId);
                    }
                }
            }
            RefreshQuestUI();
            if (questId == TrackedQuestId)
                AutoSetTrackedQuest();

            if (questId == "MainQuest_003")
                SceneDataManager.Instance.LoadScene("2_TheArgentCorridor");

            // 如果当前对话任务完成，结束启动状态
            if (isQuestActive && currentInteractiveQuestId == questId)
            {
                isQuestActive = false;
                currentInteractiveQuestId = null;
            }
        }
    }

    /// <summary>
    /// 战斗失败时回退任务状态
    /// </summary>
    public void OnCombatFailed(string questId)
    {
        if (PlayerDataManager.Instance.ResetQuestToAvailable(questId))
        {
            Debug.Log($"战斗失败，任务 {questId} 已回退到可用状态");
            RefreshQuestUI();
            AutoSetTrackedQuest();
        }
        isQuestActive = false;
        currentInteractiveQuestId = null;
        waitingForInteraction = false;
    }

    /// <summary>
    /// 开始战斗任务（由触发器调用，此时任务状态必须为 Available）
    /// </summary>
    public void StartCombatQuest(string questId, Vector2 spawnCenter)
    {
        var progress = PlayerDataManager.Instance.GetQuestProgress(questId);
        if (progress == null || progress.state != QuestProgressState.Available)
        {
            Debug.LogWarning($"任务 {questId} 状态不是 Available，无法开始战斗");
            return;
        }

        if (!GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData))
        {
            Debug.LogError($"任务 {questId} 不存在");
            return;
        }

        // 标记启动状态（运行时）
        isQuestActive = true;
        currentInteractiveQuestId = questId;

        // 调用战斗管理器开始战斗
        CombatManager.Instance.StartCombat(questData, spawnCenter);
    }

    // ----- 对话任务内部逻辑 -----
    private void StartDialogue(List<DialogueEntry> dialogueList, string questId)
    {
        if (dialogueList == null || dialogueList.Count == 0)
        {
            CompleteQuest(questId);
            return;
        }

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        if (playerController != null)
            playerController.enabled = false;

        currentDialogue = dialogueList;
        currentDialogueIndex = 0;
        isDialoguePlaying = true;
        dialoguePanel.SetActive(true);
        ShowDialogueEntry(currentDialogue[0]);
    }

    private void ShowDialogueEntry(DialogueEntry entry)
    {
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(entry.background != null);
            if (entry.background != null)
                backgroundImage.sprite = entry.background;
        }

        speakerText.text = GetSpeakerName(entry.speakerId);
        dialogueContentText.text = "";
        isTextFullyDisplayed = false;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(entry.content));
    }

    private IEnumerator TypeText(string fullText)
    {
        foreach (char c in fullText)
        {
            dialogueContentText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
        isTextFullyDisplayed = true;
    }

    private string GetSpeakerName(string speakerId) => speakerId;

    private void EndDialogue()
    {
        isDialoguePlaying = false;
        dialoguePanel.SetActive(false);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        if (!string.IsNullOrEmpty(currentInteractiveQuestId))
        {
            CompleteQuest(currentInteractiveQuestId);
            currentInteractiveQuestId = null;
        }
        isQuestActive = false;
        waitingForInteraction = false;
    }

    // ----- 区域触发逻辑 -----
    public void OnPlayerEnterQuestArea(string questId)
    {
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null)
        {
            // 任务未激活，检查前置任务是否完成，若完成则解锁
            if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData))
            {
                if (!string.IsNullOrEmpty(questData.lastQuestId) &&
                    PlayerDataManager.Instance.HasCompletedQuest(questData.lastQuestId))
                {
                    UnlockQuest(questId);
                    progress = PlayerDataManager.Instance.GetQuestProgress(questId);
                }
                else
                {
                    return; // 未解锁，无提示
                }
            }
        }

        if (progress != null && progress.state == QuestProgressState.Available)
        {
            currentInteractiveQuestId = questId;
            waitingForInteraction = true;
            Debug.Log("按F开始任务");
        }
    }

    public void OnPlayerExitQuestArea(string questId)
    {
        if (currentInteractiveQuestId == questId)
        {
            currentInteractiveQuestId = null;
            waitingForInteraction = false;
        }
    }

    // ----- UI 刷新与追踪 -----
    private void RefreshQuestUI()
    {
        var availableQuests = PlayerDataManager.Instance?.GetAvailableQuests();
        if (availableQuests == null) return;

        // 显示第一个主线任务作为追踪
        var mainQuest = availableQuests.Find(q =>
        {
            if (GameDataManager.Instance.QuestDict.TryGetValue(q.questId, out var qd))
                return qd.category == QuestCategory.Main;
            return false;
        });

        if (mainQuest != null)
        {
            var questData = GameDataManager.Instance.QuestDict[mainQuest.questId];
            guestText.text = questData.questName;
        }
        else
        {
            guestText.text = "暂无主线任务";
        }
    }

    private void AutoSetTrackedQuest()
    {
        var availableQuests = PlayerDataManager.Instance?.GetAvailableQuests();
        if (availableQuests == null) return;

        var mainQuest = availableQuests.Find(q =>
        {
            if (GameDataManager.Instance.QuestDict.TryGetValue(q.questId, out var qd))
                return qd.category == QuestCategory.Main;
            return false;
        });

        if (mainQuest != null)
            SetTrackedQuest(mainQuest.questId);
        else
            SetTrackedQuest(null);
    }

    public void SetTrackedQuest(string questId)
    {
        if (TrackedQuestId == questId) return;
        TrackedQuestId = questId;
        OnTrackedQuestChanged?.Invoke(questId);
    }

    private void ShowPanel(string taskName, string status)
    {
        if (!enablePanelAnimation) return;
        if (questPanel == null || panelTaskNameText == null || panelStatusText == null)
        {
            Debug.LogWarning("面板UI未设置");
            return;
        }

        panelTaskNameText.text = taskName;
        panelStatusText.text = status;

        if (panelCoroutine != null)
            StopCoroutine(panelCoroutine);
        panelCoroutine = StartCoroutine(PanelFadeRoutine());
    }

    private IEnumerator PanelFadeRoutine()
    {
        questPanel.SetActive(true);
        panelCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = elapsed / fadeDuration;
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayTime - fadeDuration * 2);

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;
        questPanel.SetActive(false);
        panelCoroutine = null;
    }

    // 保留 OnEnemyKilled 用于战斗任务的目标计数（如果需要）
    public void OnEnemyKilled(string enemyId)
    {
        var availableQuests = PlayerDataManager.Instance?.GetAvailableQuests();
        if (availableQuests == null) return;

        bool anyProgress = false;
        foreach (var questProgress in availableQuests)
        {
            if (!GameDataManager.Instance.QuestDict.TryGetValue(questProgress.questId, out var questData))
                continue;
            if (questData.contentType != QuestContentType.Combat)
                continue;

            bool objectiveUpdated = false;
            foreach (var objProgress in questProgress.objectives)
            {
                var objDefine = questData.objectives.Find(o => o.objectiveId == objProgress.objectiveId);
                if (objDefine == null) continue;
                if (objDefine.type == QuestObjectiveType.KillEnemy && objDefine.targetId == enemyId)
                {
                    if (!objProgress.isCompleted)
                    {
                        int newAmount = objProgress.currentAmount + 1;
                        PlayerDataManager.Instance.UpdateObjective(questProgress.questId, objProgress.objectiveId, 1);
                        if (newAmount >= objDefine.requiredAmount)
                        {
                            PlayerDataManager.Instance.SetObjectiveCompleted(questProgress.questId, objProgress.objectiveId, true);
                        }
                        objectiveUpdated = true;
                    }
                }
            }

            if (objectiveUpdated)
            {
                anyProgress = true;
                var updatedProgress = PlayerDataManager.Instance.GetQuestProgress(questProgress.questId);
                if (updatedProgress != null && updatedProgress.objectives.TrueForAll(o => o.isCompleted))
                {
                    CompleteQuest(questProgress.questId);
                }
            }
        }

        if (anyProgress)
        {
            RefreshQuestUI();
        }
    }
}