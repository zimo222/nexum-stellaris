using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
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
    public GameObject interationButton;
    public GameObject Arrow;
    public GameObject tipText;

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
    public bool isDialoguePlaying;
    private Coroutine typingCoroutine;
    private bool isTextFullyDisplayed;
    private Player playerController;
    private string currentInteractiveQuestId;
    private bool waitingForInteraction = false;
    private bool isQuestActive = false;

    private TMP_Text currentDialogueText;
    private GameObject currentDialogueFrame;

    public string TrackedQuestId { get; private set; }
    public event Action<string> OnTrackedQuestChanged;

    private QuestControlExecutor controlExecutor;

    private bool isInDialogue = false;
    private bool isExecutingControls = false;


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

        controlExecutor = GetComponent<QuestControlExecutor>();
        if (controlExecutor == null)
            controlExecutor = gameObject.AddComponent<QuestControlExecutor>();
    }

    public void FindPlayer()
    {
        while (playerController == null)
        {
            var players = GameObject.FindGameObjectsWithTag("Player");
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
                }
                if (GameDataManager.Instance.QuestDict.TryGetValue(progress.questId, out var questData) &&
                    questData.contentType == QuestContentType.Combat && questData.objectives != null)
                {
                    if (progress.objectives.Count != questData.objectives.Count)
                    {
                        progress.objectives.Clear();
                        foreach (var objDefine in questData.objectives)
                            progress.objectives.Add(new ObjectiveProgress(objDefine.objectiveId, 0, objDefine.requiredAmount, false));
                        dataChanged = true;
                    }
                }
            }
            if (dataChanged)
                PlayerDataManager.Instance.SaveCurrentPlayerData();
        }
        RefreshQuestUI();
        AutoSetTrackedQuest();
    }

    void Update()
    {
        //Debug.Log("currentInteractiveQuestId: " + currentInteractiveQuestId);
        if (isDialoguePlaying && Input.GetKeyDown(KeyCode.Space))
        {

            if (isExecutingControls) return;  // 新增：执行控制时忽略空格
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                currentDialogueText.text = currentDialogue[currentDialogueIndex].content;
                isTextFullyDisplayed = true;
                //Debug.Log(isTextFullyDisplayed.ToString() + " " + currentDialogueIndex);
            }
            else if (isTextFullyDisplayed)
            {
                var entry = currentDialogue[currentDialogueIndex];
                if (entry.controls != null && entry.controls.Count > 0)
                {
                    StartCoroutine(ExecuteControlsThenNext(entry.controls));
                }
                else
                {
                    Debug.Log(currentDialogueIndex.ToString() + " " + currentDialogue.Count.ToString());
                    currentDialogueIndex++;
                    if (currentDialogueIndex < currentDialogue.Count)
                        ShowDialogueEntry(currentDialogue[currentDialogueIndex]);
                    else
                        EndDialogue();
                }
                //Debug.Log(isTextFullyDisplayed.ToString() + " " + currentDialogueIndex);
            }
            
        }

        if (waitingForInteraction && Input.GetKeyDown(KeyCode.F))
        {
            if (playerController != null && !playerController.isIdle) return;
            if (!string.IsNullOrEmpty(currentInteractiveQuestId))
            {
                var progress = PlayerDataManager.Instance?.GetQuestProgress(currentInteractiveQuestId);
                if (progress != null && progress.state == QuestProgressState.Available &&
                    GameDataManager.Instance.QuestDict.TryGetValue(currentInteractiveQuestId, out var questData))
                {
                    if (questData.contentType == QuestContentType.Dialogue)
                    {
                        var zones = FindObjectsOfType<QuestTriggerZone>();
                        foreach (var zone in zones)
                            if (zone.questId == currentInteractiveQuestId)
                                zone.DisableButton();
                        waitingForInteraction = false;
                        isQuestActive = true;
                        StartDialogue(questData.dialogueEntries, currentInteractiveQuestId);
                        Debug.Log("currentInteractiveQuestId: " + currentInteractiveQuestId);
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

    private IEnumerator ExecuteControlsThenNext(List<QuestControl> controls)
    {
        isExecutingControls = true;
        try
        {
            if (backgroundImage != null) backgroundImage.gameObject.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            /*
            // 带超时的控制指令执行（最多10秒）
            var execCoroutine = StartCoroutine(controlExecutor.ExecuteControls(controls));
            float startTime = Time.time;
            while (Time.time - startTime < 10f)
            {
                // 检查协程是否结束（简单方式：等待一帧）
                yield return null;
                // 因为无法直接检测协程状态，我们只能假设10秒后强制跳出
            }
            Debug.Log("控制指令执行完毕或超时，继续对话");
            */

            // 执行控制指令（等待完成）
            yield return controlExecutor.ExecuteControls(controls);
        }
        finally
        {
            isExecutingControls = false;
        }
        currentDialogueIndex++;
        if (currentDialogueIndex < currentDialogue.Count)
            ShowDialogueEntry(currentDialogue[currentDialogueIndex]);
        else
            EndDialogue();
    }

    public void StartCurrentQuest()
    {
        if (!waitingForInteraction || string.IsNullOrEmpty(currentInteractiveQuestId)) return;
        if (playerController != null && !playerController.isIdle) return;
        var progress = PlayerDataManager.Instance?.GetQuestProgress(currentInteractiveQuestId);
        if (progress == null || progress.state != QuestProgressState.Available) return;
        if (!GameDataManager.Instance.QuestDict.TryGetValue(currentInteractiveQuestId, out var questData)) return;
        waitingForInteraction = false;
        if (questData.contentType == QuestContentType.Dialogue)
        {
            isQuestActive = true;
            StartDialogue(questData.dialogueEntries, currentInteractiveQuestId);
            JinYong(questData.id);
        }
    }

    public void JinYong(string questId)
    {
        Debug.Log("禁用");
        // 在场景中查找指定名称的 GameObject
        GameObject targetUI = GameObject.Find(questId);
        if (targetUI == null)
        {
            Debug.LogWarning($"未找到名为 {questId} 的 UI 对象");
            return;
        }

        if (interationButton == null)
        {
            Debug.LogWarning($"未找到名为 {questId} 的 UI 对象");
            return;
        }
        interationButton.gameObject.SetActive(false);
        Arrow.gameObject.SetActive(false);

        // 查找名为 "Image" 的子物体并禁用
        Transform imageChild = targetUI.transform.Find("Image/DefaultImage");
        if (imageChild != null)
            imageChild.gameObject.SetActive(false);
        else
            Debug.LogWarning($"在 {questId} 下未找到 Image 子物体");

        // 查找名为 "Light2D" 的子物体并禁用
        Transform light2DChild = targetUI.transform.Find("Image/Light 2D");
        if (light2DChild != null)
            light2DChild.gameObject.SetActive(false);
        else
            Debug.LogWarning($"在 {questId} 下未找到 Light2D 子物体");


        tipText.SetActive(true);
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
                        if (!PlayerDataManager.Instance.HasCompletedQuest(nextId))
                            UnlockQuest(nextId);
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
                switch (questData.id)
                {
                    case "MainQuest_001001": TutorialManager.Instance.StartTutorial("002"); break;
                    case "MainQuest_001002": TutorialManager.Instance.StartTutorial("003"); break;
                }
                if (questId[0] == 'M')
                {
                    ParseMainString(questId.AsSpan(), out int a, out int b);
                    knowledgeBase.SetProgress(a, b);
                }
                PlayerDataManager.Instance.AddExperience(questData.exp);
            }
            RefreshQuestUI();
            if (questId == TrackedQuestId) AutoSetTrackedQuest();
            if (GameDataManager.Instance.QuestDict[questId].isSceneTrans == YesNo.Yes)
            {
                SceneDataManager.Instance.LoadScene(
                    GameDataManager.Instance.QuestDict[questId].targetSceneName,
                    GameDataManager.Instance.QuestDict[questId].targetX,
                    GameDataManager.Instance.QuestDict[questId].targetY);
            }
            if (isQuestActive && currentInteractiveQuestId == questId)
            {
                isQuestActive = false;
                currentInteractiveQuestId = null;
            }
            if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var finishedQuestData) &&
                finishedQuestData.autoStartNextQuest == YesNo.Yes)
            {
                AutoStartNextQuest(finishedQuestData);
            }
        }
    }

    public static void ParseMainString(ReadOnlySpan<char> input, out int first, out int second)
    {
        if (input.Length != 16 || !input.StartsWith("MainQuest_"))
            throw new FormatException("输入格式不正确");
        var firstSpan = input.Slice(10, 3);
        var secondSpan = input.Slice(13, 3);
        first = int.Parse(firstSpan);
        second = int.Parse(secondSpan);
    }

    private void AutoStartNextQuest(QuestDefineSO finishedQuestData)
    {
        if (finishedQuestData.nextQuestIds == null || finishedQuestData.nextQuestIds.Count == 0) return;
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
        if (string.IsNullOrEmpty(nextQuestId)) return;
        if (!GameDataManager.Instance.QuestDict.TryGetValue(nextQuestId, out var nextQuestData)) return;
        waitingForInteraction = false;
        StartCoroutine(AutoStartCoroutine(nextQuestId, nextQuestData));
    }

    private IEnumerator AutoStartCoroutine(string questId, QuestDefineSO questData)
    {
        yield return null;
        if (questData.contentType == QuestContentType.Dialogue)
        {
            isQuestActive = true;
            StartDialogue(questData.dialogueEntries, questId);
        }
        else if (questData.contentType == QuestContentType.Combat)
        {
            Vector2 spawnCenter = playerController != null ? playerController.transform.position : Vector2.zero;
            StartCombatQuest(questId, spawnCenter);
        }
    }

    public void OnCombatFailed(string questId)
    {
        if (PlayerDataManager.Instance.ResetQuestToAvailable(questId))
        {
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
        if (progress == null || progress.state != QuestProgressState.Available) return;
        if (!GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData)) return;
        isQuestActive = true;
        currentInteractiveQuestId = questId;
        CombatManager.Instance.StartCombat(questData, spawnCenter);
    }

    private void StartDialogue(List<DialogueEntry> dialogueList, string questId)
    {

        isInDialogue = true;  // 新增
        Debug.Log("开始对话");
        JinYong(questId);
        if (dialogueList == null || dialogueList.Count == 0)
        {
            CompleteQuest(questId);
            return;
        }
        currentInteractiveQuestId = questId;
        if (currentDialogueFrame != null) currentDialogueFrame.SetActive(false);
        if (backgroundImage != null) backgroundImage.gameObject.SetActive(false);
        
        if (playerController != null)
        {
            playerController.enabled = false;
            playerController.gameObject.SetActive(false); // 隐藏原玩家
        }
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

            Debug.Log("currentInteractiveQuestId:" + currentInteractiveQuestId);
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(entry.background != null);
                if (entry.background != null) backgroundImage.sprite = entry.background;
            }
            speakerText.text = GetSpeakerName(entry.speakerId);
            dialogueContentText.text = "";
            currentDialogueText = dialogueContentText;
            isTextFullyDisplayed = false;
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(entry.content));

        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (backgroundImage != null) backgroundImage.gameObject.SetActive(false);
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
                Debug.LogError($"未找到 dialogueFrame");
                EndDialogue();
                return;
            }
            Transform nameTextTrans = frameTrans.Find("NameText");
            Transform dialogueTextTrans = frameTrans.Find("DialogueText");
            if (nameTextTrans == null || dialogueTextTrans == null)
            {
                Debug.LogError("未找到 NameText 或 DialogueText");
                EndDialogue();
                return;
            }
            TMP_Text nameText = nameTextTrans.GetComponent<TMP_Text>();
            TMP_Text dialogText = dialogueTextTrans.GetComponent<TMP_Text>();
            if (nameText == null || dialogText == null)
            {
                Debug.LogError("TMP_Text 组件缺失");
                EndDialogue();
                return;
            }
            frameTrans.gameObject.SetActive(true);
            currentDialogueFrame = frameTrans.gameObject;
            nameText.text = GetSpeakerName(entry.speakerId);
            dialogText.text = "";
            currentDialogueText = dialogText;
            isTextFullyDisplayed = false;
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(entry.content));
        }
    }
    /*
    private Transform FindNPCDialogueRoot(string speakerId)
    {
        if (string.IsNullOrEmpty(speakerId)) return null;
        if (speakerId == "Player")
            return GameObject.FindGameObjectWithTag("Player")?.transform;
        var npcs = FindObjectsOfType<NPCIdentifier>();
        foreach (var npc in npcs)
            if (npc.speakerId == speakerId)
                return npc.transform;
        return null;
    }
    */
    private Transform FindNPCDialogueRoot(string speakerId)
    {
        if (string.IsNullOrEmpty(speakerId)) return null;
        if (speakerId == "Player") return GameObject.Find("StoryPlayer(Clone)").transform;
            //return GameObject.FindGameObjectWithTag("Player")?.transform;
        var npcs = FindObjectsOfType<NPCIdentifier>();
        foreach (var npc in npcs)
            if (npc.HasId(speakerId))
                return npc.transform;
        return null;
    }

    private IEnumerator TypeText(string fullText)
    {
        if (currentDialogueText == null)
        {
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

    private string GetSpeakerName(string speakerId) =>
        speakerId == "Player" ? PlayerDataManager.Instance.GetCurrentUsername() : speakerId;

    private void EndDialogue()
    {
        tipText.SetActive(false);
        Arrow.gameObject.SetActive(true);
        isDialoguePlaying = false;
        Debug.Log("结束对话");
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (currentDialogueFrame != null) currentDialogueFrame.SetActive(false);
        if (backgroundImage != null) backgroundImage.gameObject.SetActive(false);
        currentDialogueText = null;
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.gameObject.SetActive(true);
        }
        Debug.Log("currentInteractiveQuestId:" + currentInteractiveQuestId);
        if (!string.IsNullOrEmpty(currentInteractiveQuestId))
        {
            string completedId = currentInteractiveQuestId;
            currentInteractiveQuestId = null;
            Debug.Log("进行CompleteQuest");
            CompleteQuest(completedId);
        }
        isQuestActive = false;
        waitingForInteraction = false;
    }

    public void OnPlayerEnterQuestArea(string questId, Vector2? spawnCenter = null)
    {
        var progress = PlayerDataManager.Instance?.GetQuestProgress(questId);
        if (progress == null)
        {
            if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var questData) &&
                !string.IsNullOrEmpty(questData.lastQuestId) &&
                PlayerDataManager.Instance.HasCompletedQuest(questData.lastQuestId))
            {
                UnlockQuest(questId);
                progress = PlayerDataManager.Instance.GetQuestProgress(questId);
            }
            else return;
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
        if (isInDialogue) return;  // 新增
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
            GameDataManager.Instance.QuestDict.TryGetValue(q.questId, out var qd) && qd.category == QuestCategory.Main);
        guestText.text = mainQuest != null ? GameDataManager.Instance.QuestDict[mainQuest.questId].questName : "暂无主线任务";
    }

    private void AutoSetTrackedQuest()
    {
        var availableQuests = PlayerDataManager.Instance?.GetAvailableQuests();
        if (availableQuests == null) return;
        var mainQuest = availableQuests.Find(q =>
            GameDataManager.Instance.QuestDict.TryGetValue(q.questId, out var qd) && qd.category == QuestCategory.Main);
        SetTrackedQuest(mainQuest != null ? mainQuest.questId : null);
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
        if (questPanel == null || panelTaskNameText == null || panelStatusText == null) return;
        panelTaskNameText.text = taskName;
        panelStatusText.text = status;
        if (panelCoroutine != null) StopCoroutine(panelCoroutine);
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
            if (!GameDataManager.Instance.QuestDict.TryGetValue(questProgress.questId, out var questData)) continue;
            if (questData.contentType != QuestContentType.Combat) continue;
            bool objectiveUpdated = false;
            foreach (var objProgress in questProgress.objectives)
            {
                var objDefine = questData.objectives.Find(o => o.objectiveId == objProgress.objectiveId);
                if (objDefine == null) continue;
                if (objDefine.type == QuestObjectiveType.KillEnemy && objDefine.targetId == enemyId)
                {
                    if (!objProgress.isCompleted)
                    {
                        PlayerDataManager.Instance.UpdateObjective(questProgress.questId, objProgress.objectiveId, 1);
                        if (objProgress.currentAmount + 1 >= objDefine.requiredAmount)
                            PlayerDataManager.Instance.SetObjectiveCompleted(questProgress.questId, objProgress.objectiveId, true);
                        objectiveUpdated = true;
                    }
                }
            }
            if (objectiveUpdated)
            {
                anyProgress = true;
                var updatedProgress = PlayerDataManager.Instance.GetQuestProgress(questProgress.questId);
                if (updatedProgress != null && updatedProgress.objectives.TrueForAll(o => o.isCompleted))
                    CompleteQuest(questProgress.questId);
            }
        }
        if (anyProgress) RefreshQuestUI();
    }
}