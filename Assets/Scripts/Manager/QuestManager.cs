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
    private bool isQuestActive = false;

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

        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData != null)
        {
            bool dataChanged = false;
            foreach (var progress in playerData.activeQuests)
            {
                if (progress.state != QuestProgressState.Available && progress.state != QuestProgressState.Completed)
                {
                    progress.state = QuestProgressState.Available;
                    dataChanged = true;
                    Debug.Log($"修复任务状态: {progress.questId} -> Available");
                }

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
        // 对话空格处理
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

        // 按F启动任务（原有逻辑，修改了对话部分）
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
                        // 查找对应的触发器并禁用按钮
                        QuestTriggerZone[] zones = FindObjectsOfType<QuestTriggerZone>();
                        foreach (var zone in zones)
                        {
                            if (zone.questId == currentInteractiveQuestId)
                            {
                                zone.DisableButton();
                                break;
                            }
                        }

                        waitingForInteraction = false;
                        isQuestActive = true;
                        StartDialogue(questData.dialogueEntries, currentInteractiveQuestId);
                    }
                    else if (questData.contentType == QuestContentType.Combat)
                    {
                        waitingForInteraction = false;
                        isQuestActive = true;
                        // 战斗由 CombatQuestTrigger 启动，这里不处理
                    }
                }
            }
        }
    }

    // ========== 新增：供UI按钮调用的公共方法 ==========
    public void StartCurrentQuest()
    {
        if (!waitingForInteraction || string.IsNullOrEmpty(currentInteractiveQuestId))
            return;

        if (playerController != null && !playerController.isIdle)
        {
            Debug.Log("移动中不能开始任务");
            return;
        }

        var progress = PlayerDataManager.Instance?.GetQuestProgress(currentInteractiveQuestId);
        if (progress == null || progress.state != QuestProgressState.Available)
            return;

        if (!GameDataManager.Instance.QuestDict.TryGetValue(currentInteractiveQuestId, out var questData))
            return;

        waitingForInteraction = false;

        if (questData.contentType == QuestContentType.Dialogue)
        {
            isQuestActive = true;
            StartDialogue(questData.dialogueEntries, currentInteractiveQuestId);
        }
        else if (questData.contentType == QuestContentType.Combat)
        {
            // 战斗任务不应该通过此方法启动，但为了安全，不做任何事
            Debug.LogWarning("StartCurrentQuest 不应启动战斗任务");
        }
    }

    // ========== 原有方法（未修改） ==========
    public void UnlockQuest(string questId)
    {
        if (PlayerDataManager.Instance.UnlockQuest(questId))
        {
            RefreshQuestUI();
            AutoSetTrackedQuest();
        }
    }

    public void CompleteQuest(string questId)
    {
        if (PlayerDataManager.Instance.CompleteQuest(questId))
        {
            if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData))
            {
                ShowPanel(questData.questName, "完成");

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

                if (questData.Reward != null)
                {
                    List<string> rewardIds = new List<string>();
                    foreach (string rewardId in questData.Reward)
                    {
                        rewardIds.Add(rewardId);
                        if (rewardId[0] == 'E')
                            PlayerDataManager.Instance.AddExotext(rewardId);
                        else
                            PlayerDataManager.Instance.AddNexusVesture(rewardId);
                    }
                    // 调用功能UI组件
                    if (ItemObtainDisplayUI.Instance != null)
                        ItemObtainDisplayUI.Instance.ShowItemRewards(rewardIds);
                }
            }
            RefreshQuestUI();
            if (questId == TrackedQuestId)
                AutoSetTrackedQuest();
            /*
            if (questId == "MainQuest_002001")
                SceneDataManager.Instance.LoadScene("2_TheArgentCorridor");
            */
            if (GameDataManager.Instance.QuestDict[questId].isSceneTrans == YesNo.Yes)
                SceneDataManager.Instance.LoadScene(GameDataManager.Instance.QuestDict[questId].targetSceneName, GameDataManager.Instance.QuestDict[questId].targetX, GameDataManager.Instance.QuestDict[questId].targetY);

            if (isQuestActive && currentInteractiveQuestId == questId)
            {
                isQuestActive = false;
                currentInteractiveQuestId = null;
            }
        }
    }

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

        isQuestActive = true;
        currentInteractiveQuestId = questId;
        CombatManager.Instance.StartCombat(questData, spawnCenter);
    }

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

    private string GetSpeakerName(string speakerId) => speakerId == "Player" ? PlayerDataManager.Instance.GetCurrentUsername() : speakerId;

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

    public void OnPlayerEnterQuestArea(string questId, Vector2? spawnCenter = null)
    {
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null)
        {
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
                    return;
                }
            }
        }

        if (progress != null && progress.state == QuestProgressState.Available)
        {
            currentInteractiveQuestId = questId;
            waitingForInteraction = true;
            Debug.Log("按F或点击按钮开始任务");
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

    private void RefreshQuestUI()
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