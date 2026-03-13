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

    void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players != null && players.Length > 0)
        {
            playerController = players[0].GetComponent<Player>();
        }

        // 修复直接从 PlayerData 添加的任务
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData != null)
        {
            bool dataChanged = false;
            foreach (var progress in playerData.activeQuests)
            {
                if (progress.state == QuestState.NotStarted)
                {
                    if (GameDataManager.Instance.QuestDict.TryGetValue(progress.questId, out var questData))
                    {
                        progress.state = QuestState.InProgress;
                        if (questData.contentType == QuestContentType.Combat && questData.objectives != null)
                        {
                            progress.objectives.Clear();
                            foreach (var objDefine in questData.objectives)
                            {
                                var objProgress = new ObjectiveProgress(objDefine.objectiveId, 0, objDefine.requiredAmount, false);
                                progress.objectives.Add(objProgress);
                            }
                        }
                        dataChanged = true;
                        Debug.Log($"修复任务: {progress.questId} 状态为 InProgress");
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

        // 等待玩家按F键开始对话
        if (waitingForInteraction && Input.GetKeyDown(KeyCode.F))
        {
            if (playerController != null && !playerController.isIdle)
            {
                Debug.Log("移动中不能开始对话");
                return;
            }

            if (!string.IsNullOrEmpty(currentInteractiveQuestId))
            {
                var progress = PlayerDataManager.Instance?.GetQuestProgress(currentInteractiveQuestId);
                if (progress != null && GameDataManager.Instance.QuestDict.TryGetValue(currentInteractiveQuestId, out var questData))
                {
                    if (questData.contentType == QuestContentType.Dialogue)
                    {
                        waitingForInteraction = false;
                        StartDialogue(questData.dialogueEntries, currentInteractiveQuestId);
                    }
                }
            }
        }
    }

    public void ActivateQuest(string questId)
    {
        if (!GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData))
        {
            Debug.LogError($"任务 {questId} 不存在");
            return;
        }

        var progress = new PlayerQuestProgress(questId);
        progress.state = QuestState.InProgress;
        if (questData.contentType == QuestContentType.Combat && questData.objectives != null)
        {
            foreach (var objDefine in questData.objectives)
            {
                var objProgress = new ObjectiveProgress(objDefine.objectiveId, 0, objDefine.requiredAmount, false);
                progress.objectives.Add(objProgress);
            }
        }

        if (PlayerDataManager.Instance.AddActiveQuest(progress))
        {
            ShowPanel(questData.questName, "开始");
            RefreshQuestUI();
            AutoSetTrackedQuest();
        }
    }

    public void OnEnemyKilled(string enemyId)
    {
        var activeQuests = PlayerDataManager.Instance?.GetActiveQuests();
        if (activeQuests == null) return;

        bool anyProgress = false;
        foreach (var questProgress in activeQuests)
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

    private void CompleteQuest(string questId)
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
                            ActivateQuest(nextId);
                        }
                    }
                }
            }
            RefreshQuestUI();
            if (questId == TrackedQuestId)
            {
                AutoSetTrackedQuest();
            }
        }
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
            if (entry.background != null)
            {
                backgroundImage.sprite = entry.background;
                backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                backgroundImage.gameObject.SetActive(false);
            }
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
    }

    public void OnPlayerEnterQuestArea(string questId)
    {
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null)
        {
            if (!PlayerDataManager.Instance.HasCompletedQuest(questId))
            {
                ActivateQuest(questId);
            }
            return;
        }

        if (!GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData))
            return;

        if (questData.contentType == QuestContentType.Dialogue)
        {
            currentInteractiveQuestId = questId;
            waitingForInteraction = true;
            Debug.Log("按F开始对话");
        }
        else if (questData.contentType == QuestContentType.Combat)
        {
            Debug.Log("进入战斗区域");
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
        var activeQuests = PlayerDataManager.Instance?.GetActiveQuests();
        if (activeQuests == null) return;

        var mainQuestProgress = activeQuests.Find(q =>
        {
            if (GameDataManager.Instance.QuestDict.TryGetValue(q.questId, out var qd))
                return qd.category == QuestCategory.Main;
            return false;
        });

        if (mainQuestProgress != null)
        {
            var questData = GameDataManager.Instance.QuestDict[mainQuestProgress.questId];
            guestText.text = questData.questName;
        }
        else
        {
            guestText.text = "暂无主线任务";
        }
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

    // ========== 追踪设置方法 ==========
    public void SetTrackedQuest(string questId)
    {
        if (TrackedQuestId == questId) return;
        TrackedQuestId = questId;
        int subscriberCount = OnTrackedQuestChanged?.GetInvocationList()?.Length ?? 0;
        Debug.Log($"追踪任务变更为: {questId}，当前事件订阅者数量: {subscriberCount}");
        OnTrackedQuestChanged?.Invoke(questId);
    }

    private void AutoSetTrackedQuest()
    {
        var activeQuests = PlayerDataManager.Instance?.GetActiveQuests();
        if (activeQuests == null) return;

        var mainQuest = activeQuests.Find(q =>
        {
            if (GameDataManager.Instance.QuestDict.TryGetValue(q.questId, out var qd))
            {
                bool isMain = qd.category == QuestCategory.Main;
                if (isMain) Debug.Log($"找到主线任务: {q.questId}");
                return isMain;
            }
            return false;
        });

        if (mainQuest != null)
        {
            SetTrackedQuest(mainQuest.questId);
        }
        else
        {
            Debug.Log("未找到主线任务，清空追踪");
            SetTrackedQuest(null);
        }
    }
}