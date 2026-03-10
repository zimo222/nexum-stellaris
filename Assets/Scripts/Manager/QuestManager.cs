using DG.Tweening.Core.Easing;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("UI 引用")]
    public TMP_Text guestText;               // 常驻任务名
    public GameObject questPanel;         // 弹出面板
    public TMP_Text panelTaskNameText;        // 面板上的任务名
    public TMP_Text panelStatusText;          // 面板上的状态（开始/完成）

    [Header("动画参数")]
    public float fadeDuration = 0.3f;     // 渐入渐出时间
    public float displayTime = 1f;        // 面板总显示时间（含动画）

    private CanvasGroup panelCanvasGroup;
    private Coroutine panelCoroutine;

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
    }

    void Start()
    {
        // 游戏启动时刷新显示并检查进度
        RefreshCurrentQuestDisplay();

        // 检查当前任务进度，决定是否弹出提示
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData != null && !string.IsNullOrEmpty(playerData.currentQuestId))
        {
            if (playerData.currentQuestProgress >= 1f)
            {
                // 进度已满 → 完成任务（会自动弹出完成并开始下一个）
                CompleteCurrentQuest();
            }
            else if (playerData.currentQuestProgress == 0f)
            {
                // 进度为0 → 弹出“开始”提示
                ShowPanel(GetQuestName(playerData.currentQuestId), "开始");
            }
        }
    }

    // 刷新常驻任务名显示
    public void RefreshCurrentQuestDisplay()
    {
        if (PlayerDataManager.Instance?.CurrentPlayerData == null) return;

        string currentId = PlayerDataManager.Instance.CurrentPlayerData.currentQuestId;
        if (string.IsNullOrEmpty(currentId))
        {
            guestText.text = ""; // 无任务
            return;
        }
        Debug.Log(currentId);
        var questData = GameDataManager.Instance.QuestDict[currentId];


        if (questData != null)
        {
            guestText.text = questData.questName;
        }
        else
        {
            Debug.LogError($"任务ID {currentId} 不存在");
            guestText.text = "未知任务";
        }
    }

    // 开始一个新任务（通常由完成上一个任务时自动调用）
    private void StartQuest(string questId)
    {
        if (PlayerDataManager.Instance?.CurrentPlayerData == null) return;
        if (!GameDataManager.Instance.QuestDict.ContainsKey(questId))
        {
            Debug.LogError($"任务 {questId} 不存在");
            return;
        }

        // 更新玩家数据
        PlayerDataManager.Instance.CurrentPlayerData.currentQuestId = questId;
        PlayerDataManager.Instance.CurrentPlayerData.currentQuestProgress = -1;

        // 刷新常驻文本
        RefreshCurrentQuestDisplay();

        // 弹出“开始”提示
        ShowPanel(GetQuestName(questId), "开始");
    }

    // 完成当前任务（外部调用：比如任务目标达成时）
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

        // 弹出“完成”提示
        ShowPanel(questData.questName, "完成");

        // 标记进度为1（可选）
        playerData.currentQuestProgress = 100;

        // 自动推进到下一个任务
        if (!string.IsNullOrEmpty(questData.nextQuestId))
        {
            StartQuest(questData.nextQuestId);
        }
        else
        {
            // 主线完结，清除当前任务
            playerData.currentQuestId = null;
            RefreshCurrentQuestDisplay();
        }
    }

    // 更新当前任务进度（由外部事件调用，如击杀敌人）
    public void UpdateCurrentQuestProgress(int delta)
    {
        var playerData = PlayerDataManager.Instance?.CurrentPlayerData;
        if (playerData == null || string.IsNullOrEmpty(playerData.currentQuestId)) return;

        playerData.currentQuestProgress += delta;

        // 如果进度达到或超过1，自动完成
        if (playerData.currentQuestProgress >= 1f)
        {
            CompleteCurrentQuest();
        }
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

        // 渐入
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            panelCanvasGroup.alpha = elapsed / fadeDuration;
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        // 停留（总显示时间减去渐入渐出时间的一半？这里简单固定）
        yield return new WaitForSeconds(displayTime - fadeDuration * 2);

        // 渐出
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

    // 辅助：根据ID获取任务名
    private string GetQuestName(string questId)
    {
        if (GameDataManager.Instance.QuestDict.TryGetValue(questId, out var data))
            return data.questName;
        return "未知任务";
    }
}