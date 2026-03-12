using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("常驻UI")]
    public TMP_Text guestText;               // 常驻任务名

    [Header("弹出面板")]
    public GameObject questPanel;
    public TMP_Text panelTaskNameText;
    public TMP_Text panelStatusText;

    [Header("对话UI")]
    public GameObject dialoguePanel;          // 对话面板（整体）
    public Image backgroundImage;             // 背景图
    public TMP_Text speakerText;              // 说话者名称
    public TMP_Text dialogueContentText;      // 对话内容

    [Header("动画参数")]
    public float fadeDuration = 0.3f;
    public float displayTime = 1f;
    public bool enablePanelAnimation = true;   // true: 显示面板并播放动画；false: 完全禁用面板

    [Header("打字效果")]
    public float typingSpeed = 0.05f;          // 每个字符出现的时间间隔

    private CanvasGroup panelCanvasGroup;
    private Coroutine panelCoroutine;

    // 对话控制
    private List<DialogueEntry> currentDialogue;
    private int currentDialogueIndex;
    private bool isDialoguePlaying;

    // 打字效果控制
    private Coroutine typingCoroutine;
    private bool isTextFullyDisplayed;

    // 玩家控制脚本引用
    private Player playerController;

    // 等待交互状态（用于对话性任务）
    private bool waitingForInteraction = false;

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

        // 初始化背景图为禁用
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);
    }

    void Start()
    {
        RefreshCurrentQuestDisplay();

        // 启动时如果当前任务进度为-1，什么也不做（等区域触发）
        // 如果进度=100（异常情况），尝试完成
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData != null && playerData.currentQuestProgress >= 100)
        {
            CompleteCurrentQuest();
        }

        // 查找玩家控制脚本
        GameObject[] player = GameObject.FindGameObjectsWithTag("Player");
        if (player != null && player.Length > 0)
        {
            playerController = player[0].GetComponent<Player>();
            if (playerController == null)
                Debug.LogWarning("玩家身上没有找到 PlayerController 脚本");
        }
        else
        {
            Debug.LogWarning("场景中没有 Tag 为 'Player' 的对象");
        }
    }

    void Update()
    {
        // 对话进行中：处理空格键
        if (isDialoguePlaying && Input.GetKeyDown(KeyCode.Space))
        {
            if (typingCoroutine != null) // 正在打字中 → 立即显示完整文本
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                dialogueContentText.text = currentDialogue[currentDialogueIndex].content;
                isTextFullyDisplayed = true;
            }
            else if (isTextFullyDisplayed) // 已经完整显示 → 进入下一条
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

            var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
            if (playerData != null && GameDataManager.Instance.QuestDict.TryGetValue(playerData.currentQuestId, out var questData))
            {
                if (questData.contentType == QuestContentType.Dialogue)
                {
                    waitingForInteraction = false;
                    StartDialogue(questData.dialogueEntries);
                }
            }
        }
    }

    // 刷新常驻任务名
    public void RefreshCurrentQuestDisplay()
    {
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData == null) return;

        string currentId = playerData.currentQuestId;
        if (string.IsNullOrEmpty(currentId))
        {
            guestText.text = "";
            return;
        }

        if (GameDataManager.Instance.QuestDict.TryGetValue(currentId, out var questData))
        {
            guestText.text = questData.questName;
        }
        else
        {
            Debug.LogError($"任务ID {currentId} 不存在");
            guestText.text = "未知任务";
        }
    }

    // 玩家进入任务区域时调用
    public void OnPlayerEnterQuestArea(string questId)
    {
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData == null) return;

        if (playerData.currentQuestId != questId) return;

        if (playerData.currentQuestProgress == -1)
        {
            playerData.currentQuestProgress = 0;
            ShowPanel(GetQuestName(questId), "开始");

            var questData = GameDataManager.Instance.QuestDict[questId];
            if (questData.contentType == QuestContentType.Dialogue)
            {
                waitingForInteraction = true;
                Debug.Log("按F开始对话");
            }
            else if (questData.contentType == QuestContentType.Combat)
            {
                Debug.Log("战斗任务已激活，请击败敌人");
            }
        }
    }

    // 开始对话
    private void StartDialogue(List<DialogueEntry> dialogueList)
    {
        if (dialogueList == null || dialogueList.Count == 0)
        {
            CompleteCurrentQuest();
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

    // 显示一条对话
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

    // 打字效果协程
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

    private string GetSpeakerName(string speakerId)
    {
        return speakerId;
    }

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

        CompleteCurrentQuest();
    }

    public void OnEnemyKilled(string enemyId)
    {
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData == null || playerData.currentQuestProgress < 0) return;

        string currentId = playerData.currentQuestId;
        var questData = GameDataManager.Instance.QuestDict[currentId];
        if (questData.contentType != QuestContentType.Combat) return;

        foreach (var obj in questData.objectives)
        {
            if (obj.type == QuestObjectiveType.KillEnemy && obj.targetId == enemyId)
            {
                int increment = 100 / obj.requiredAmount;
                playerData.currentQuestProgress += increment;
                if (playerData.currentQuestProgress > 100)
                    playerData.currentQuestProgress = 100;
                break;
            }
        }

        if (playerData.currentQuestProgress >= 100)
        {
            CompleteCurrentQuest();
        }
    }

    public void CompleteCurrentQuest()
    {
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData == null) return;
        if (string.IsNullOrEmpty(playerData.currentQuestId)) return;

        string currentId = playerData.currentQuestId;
        if (!GameDataManager.Instance.QuestDict.TryGetValue(currentId, out var questData))
        {
            Debug.LogError($"当前任务 {currentId} 不存在");
            return;
        }

        playerData.currentQuestProgress = 100;
        ShowPanel(questData.questName, "完成");

        if (!string.IsNullOrEmpty(questData.nextQuestId))
        {
            StartQuest(questData.nextQuestId);
        }
        else
        {
            playerData.currentQuestId = null;
            RefreshCurrentQuestDisplay();
        }
    }

    private void StartQuest(string questId)
    {
        if (PlayerDataManager.Instance?.CurrentPlayerData == null) return;
        if (!GameDataManager.Instance.QuestDict.ContainsKey(questId))
        {
            Debug.LogError($"任务 {questId} 不存在");
            return;
        }

        var playerData = PlayerDataManager.Instance.CurrentPlayerData;
        playerData.currentQuestId = questId;
        playerData.currentQuestProgress = -1;
        waitingForInteraction = false;
        RefreshCurrentQuestDisplay();
        ShowPanel(GetQuestName(questId), "开始");
    }

    // 弹出面板：仅当开关打开时才显示（带淡入淡出动画）
    private void ShowPanel(string taskName, string status)
    {
        if (!enablePanelAnimation)          // 开关关闭 → 完全禁用面板
            return;

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

    private string GetQuestName(string questId)
    {
        if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var data))
            return data.questName;
        return "未知任务";
    }
}