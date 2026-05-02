using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("知识库")]
    public VectorKnowledgeBase knowledgeBase;

    [Header("常驻UI")]
    public TMP_Text guestText;

    [Header("任务面板")]
    public GameObject questPanel;
    public TMP_Text panelTaskNameText;
    public TMP_Text panelStatusText;

    [Header("对话UI（传统CG模式）")]
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

    private TMP_Text currentDialogueText;
    private GameObject currentDialogueFrame;

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
                    Debug.Log($"修正任务状态: {progress.questId} -> Available");
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
        if (isDialoguePlaying && Input.GetKeyDown(KeyCode.Space))
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                currentDialogueText.text = currentDialogue[currentDialogueIndex].content;
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
                    }
                }
            }
        }
    }

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
            Debug.LogWarning("StartCurrentQuest 不应用于战斗任务");
        }
    }

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
                    if (ItemObtainDisplayUI.Instance != null)
                        ItemObtainDisplayUI.Instance.ShowItemRewards(rewardIds);
                }

                if (questId[0] == 'M')
                {
                    ParseMainString(questId.AsSpan(), out int a, out int b);
                    knowledgeBase.SetProgress(a, b);
                }
            }

            RefreshQuestUI();
            if (questId == TrackedQuestId)
                AutoSetTrackedQuest();

            if (GameDataManager.Instance.QuestDict[questId].isSceneTrans == YesNo.Yes)
            {
                SceneDataManager.Instance.LoadScene(
                    GameDataManager.Instance.QuestDict[questId].targetSceneName,
                    GameDataManager.Instance.QuestDict[questId].targetX,
                    GameDataManager.Instance.QuestDict[questId].targetY);
            }

            // 清理当前任务标记
            if (isQuestActive && currentInteractiveQuestId == questId)
            {
                isQuestActive = false;
                currentInteractiveQuestId = null;
            }

            // 自动开始下一个任务
            if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var finishedQuestData) &&
                finishedQuestData.autoStartNextQuest == YesNo.Yes)
            {
                AutoStartNextQuest(finishedQuestData);
            }
        }
    }

    public static void ParseMainString(ReadOnlySpan<char> input, out int first, out int second)
    {
        // 格式："MainQuest_012034"
        // 索引: 0-9 "Main_", 10-12 第一个数字, 13-15 第二个数字
        if (input.Length != 16 || !input.StartsWith("MainQuest_"))
            throw new FormatException("输入格式不正确");

        // 直接切片，无内存分配
        var firstSpan = input.Slice(10, 3);
        var secondSpan = input.Slice(13, 3);

        first = int.Parse(firstSpan);
        second = int.Parse(secondSpan);
    }

    private void AutoStartNextQuest(QuestDefineSO finishedQuestData)
    {
        if (finishedQuestData.nextQuestIds == null || finishedQuestData.nextQuestIds.Count == 0)
            return;

        string nextQuestId = null;
        foreach (string nextId in finishedQuestData.nextQuestIds)
        {
            var progress = PlayerDataManager.Instance.GetQuestProgress(nextId);
            if (progress != null && progress.state == QuestProgressState.Available)
            {
                nextQuestId = nextId;
                break;
            }
        }

        if (string.IsNullOrEmpty(nextQuestId))
        {
            Debug.LogWarning($"自动开始失败：没有可用的后续任务 (questId={finishedQuestData.id})");
            return;
        }

        if (!GameDataManager.Instance.QuestDict.TryGetValue(nextQuestId, out var nextQuestData))
            return;

        Debug.Log($"自动开始下一个任务: {nextQuestId} ({nextQuestData.questName})");

        // 清理可能残留的等待交互状态
        waitingForInteraction = false;

        StartCoroutine(AutoStartCoroutine(nextQuestId, nextQuestData));
    }

    private IEnumerator AutoStartCoroutine(string questId, QuestDefineSO questData)
    {
        yield return null;

        switch (questData.contentType)
        {
            case QuestContentType.Dialogue:
                isQuestActive = true;
                StartDialogue(questData.dialogueEntries, questId);
                break;

            case QuestContentType.Combat:
                Vector2 spawnCenter = Vector2.zero;
                if (playerController != null)
                    spawnCenter = playerController.transform.position;
                else
                    Debug.LogWarning("自动开始战斗时找不到玩家，出生点使用 (0,0)");

                StartCombatQuest(questId, spawnCenter);
                break;
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
            Debug.LogError($"任务 {questId} 数据不存在");
            return;
        }

        isQuestActive = true;
        currentInteractiveQuestId = questId;   // // 战斗任务保留此赋值
        CombatManager.Instance.StartCombat(questData, spawnCenter);
    }

    // ========== 对话系统 ==========
    private void StartDialogue(List<DialogueEntry> dialogueList, string questId)
    {
        if (dialogueList == null || dialogueList.Count == 0)
        {
            CompleteQuest(questId);
            return;
        }

        // 关键修复：赋值当前任务ID，以便结束时能触发 CompleteQuest
        currentInteractiveQuestId = questId;

        if (currentDialogueFrame != null)
        {
            currentDialogueFrame.SetActive(false);
            currentDialogueFrame = null;
        }

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        if (playerController != null)
            playerController.enabled = false;

        currentDialogue = dialogueList;
        currentDialogueIndex = 0;
        isDialoguePlaying = true;

        ShowDialogueEntry(currentDialogue[0]);
    }

    private void ShowDialogueEntry(DialogueEntry entry)
    {
        if (currentDialogueFrame != null)
        {
            currentDialogueFrame.SetActive(false);
            currentDialogueFrame = null;
        }

        if (entry.useCGMode == YesNo.Yes)
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(entry.background != null);
                if (entry.background != null)
                    backgroundImage.sprite = entry.background;
            }

            speakerText.text = GetSpeakerName(entry.speakerId);
            dialogueContentText.text = "";
            currentDialogueText = dialogueContentText;
            isTextFullyDisplayed = false;

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(entry.content));
        }
        else
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);

            if (backgroundImage != null)
                backgroundImage.gameObject.SetActive(false);

            Transform npcTransform = FindNPCDialogueRoot(entry.speakerId);
            if (npcTransform == null)
            {
                Debug.LogError($"未找到说话者: {entry.speakerId}");
                EndDialogue();
                return;
            }

            Transform frameTrans = npcTransform.Find("DialogueFrame");
            if (frameTrans == null)
            {
                Debug.LogError($"在 {entry.speakerId} 下未找到 dialogueFrame 子物体");
                EndDialogue();
                return;
            }

            Transform nameTextTrans = frameTrans.Find("NameText");
            Transform dialogueTextTrans = frameTrans.Find("DialogueText");
            if (nameTextTrans == null || dialogueTextTrans == null)
            {
                Debug.LogError("dialogueFrame 下未找到 NameText 或 DialogueText");
                EndDialogue();
                return;
            }

            TMP_Text nameText = nameTextTrans.GetComponent<TMP_Text>();
            TMP_Text dialogText = dialogueTextTrans.GetComponent<TMP_Text>();
            if (nameText == null || dialogText == null)
            {
                Debug.LogError("NameText 或 DialogueText 上没有 TMP_Text 组件");
                EndDialogue();
                return;
            }

            frameTrans.gameObject.SetActive(true);
            currentDialogueFrame = frameTrans.gameObject;
            nameText.text = GetSpeakerName(entry.speakerId);
            dialogText.text = "";
            currentDialogueText = dialogText;

            isTextFullyDisplayed = false;
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(entry.content));
        }
    }

    private Transform FindNPCDialogueRoot(string speakerId)
    {
        if (string.IsNullOrEmpty(speakerId)) return null;

        if (speakerId == "Player")
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj?.transform;
        }

        NPCIdentifier[] npcs = FindObjectsOfType<NPCIdentifier>();
        foreach (var npc in npcs)
        {
            if (npc.speakerId == speakerId)
                return npc.transform;
        }
        return null;
    }

    private IEnumerator TypeText(string fullText)
    {
        if (currentDialogueText == null)
        {
            Debug.LogError("currentDialogueText 为空");
            typingCoroutine = null;
            isTextFullyDisplayed = true;
            yield break;
        }

        currentDialogueText.text = "";
        foreach (char c in fullText)
        {
            currentDialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
        isTextFullyDisplayed = true;
    }

    private string GetSpeakerName(string speakerId) => speakerId == "Player" ? PlayerDataManager.Instance.GetCurrentUsername() : speakerId;

    private void EndDialogue()
    {
        isDialoguePlaying = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (currentDialogueFrame != null)
        {
            currentDialogueFrame.SetActive(false);
            currentDialogueFrame = null;
        }

        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        currentDialogueText = null;

        if (playerController != null)
            playerController.enabled = true;

        if (!string.IsNullOrEmpty(currentInteractiveQuestId))
        {
            string completedId = currentInteractiveQuestId;
            currentInteractiveQuestId = null; // 提前清空，防止递归重入
            CompleteQuest(completedId);
        }

        isQuestActive = false;
        waitingForInteraction = false;
    }

    // ========== 触发区域交互 ==========
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

    // ========== UI刷新 ==========
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
            Debug.LogWarning("任务UI未配置");
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