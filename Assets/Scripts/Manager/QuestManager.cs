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

    private CanvasGroup panelCanvasGroup;
    private Coroutine panelCoroutine;

    // 对话控制
    private List<DialogueEntry> currentDialogue;
    private int currentDialogueIndex;
    private bool isDialoguePlaying;

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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<Player>();
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
        // 对话进行中：按空格翻页
        if (isDialoguePlaying && Input.GetKeyDown(KeyCode.Space))
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

        // 等待玩家按F键开始对话
        if (waitingForInteraction && Input.GetKeyDown(KeyCode.F))
        {
            // 检查玩家是否正在移动（如果 PlayerController 有 IsMoving 属性）
            if (!playerController.isIdle)
            {
                Debug.Log("移动中不能开始对话"); // 可以在这里播放一个提示音或显示文字
                return;
            }

            var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
            if (playerData != null && GameDataManager.Instance.QuestDict.TryGetValue(playerData.currentQuestId, out var questData))
            {
                if (questData.contentType == QuestContentType.Dialogue)
                {
                    waitingForInteraction = false; // 取消等待状态
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

        // 只处理当前任务
        if (playerData.currentQuestId != questId) return;

        // 如果进度为 -1，激活任务
        if (playerData.currentQuestProgress == -1)
        {
            playerData.currentQuestProgress = 0;
            ShowPanel(GetQuestName(questId), "开始");

            var questData = GameDataManager.Instance.QuestDict[questId];
            if (questData.contentType == QuestContentType.Dialogue)
            {
                // 对话任务：进入等待交互状态，不立即开始对话
                waitingForInteraction = true;
                // 可以在这里显示一个“按F开始对话”的提示（需额外UI）
                Debug.Log("按F开始对话");
            }
            else if (questData.contentType == QuestContentType.Combat)
            {
                // 战斗任务：可以在这里生成敌人或开启战斗区域
                Debug.Log("战斗任务已激活，请击败敌人");
            }
        }
    }

    // 开始对话
    private void StartDialogue(List<DialogueEntry> dialogueList)
    {
        if (dialogueList == null || dialogueList.Count == 0)
        {
            CompleteCurrentQuest(); // 没有对话直接完成
            return;
        }

        // 进入对话前强制关闭背景图
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        // 禁用玩家控制
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
        // 背景图
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

        // 说话者和内容
        speakerText.text = GetSpeakerName(entry.speakerId);
        dialogueContentText.text = entry.content;
    }

    // 获取说话者显示名称（简单返回ID，你可以扩展为中文名表）
    private string GetSpeakerName(string speakerId)
    {
        return speakerId; // 暂时返回ID
    }

    // 结束对话
    private void EndDialogue()
    {
        isDialoguePlaying = false;
        dialoguePanel.SetActive(false);

        // 对话结束关闭背景图
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);

        // 启用玩家控制
        if (playerController != null)
            playerController.enabled = true;

        CompleteCurrentQuest(); // 对话结束直接完成任务
    }

    // 敌人死亡时调用
    public void OnEnemyKilled(string enemyId)
    {
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData == null || playerData.currentQuestProgress < 0) return;

        string currentId = playerData.currentQuestId;
        var questData = GameDataManager.Instance.QuestDict[currentId];
        if (questData.contentType != QuestContentType.Combat) return;

        // 查找目标，找到对应敌人ID后增加进度
        foreach (var obj in questData.objectives)
        {
            if (obj.type == QuestObjectiveType.KillEnemy && obj.targetId == enemyId)
            {
                int increment = 100 / obj.requiredAmount; // 整除，每个敌人固定贡献
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

    // 完成任务
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

        // 开启下一个任务
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

    // 开始一个新任务（由CompleteCurrentQuest调用）
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
        playerData.currentQuestProgress = -1; // 需要进入区域激活

        // 新任务开始时，重置等待交互状态（因为还没有进入区域）
        waitingForInteraction = false;

        RefreshCurrentQuestDisplay();
        ShowPanel(GetQuestName(questId), "开始");
    }

    // 弹出面板（带渐变动画）
    private void ShowPanel(string taskName, string status)
    {
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

    // 辅助
    private string GetQuestName(string questId)
    {
        if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var data))
            return data.questName;
        return "未知任务";
    }
}