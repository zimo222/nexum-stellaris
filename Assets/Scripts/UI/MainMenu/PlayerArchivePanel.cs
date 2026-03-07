using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class PlayerArchivePanel : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject archiveButtonPrefab;
    [SerializeField] private Button addButton;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TMP_Text warningText;

    private List<GameObject> currentButtons = new List<GameObject>();
    private Coroutine _warningCoroutine;

    // 双击检测相关
    private Coroutine _clickCoroutine;
    private string _pendingPlayerID;
    private float doubleClickTime = 0.3f;

    // 重命名相关
    private string _renamingPlayerID = null;
    private bool _isRenaming = false;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager 不存在！");
            return;
        }

        PlayerDataManager.Instance.OnPlayerDataListChanged += RefreshUI;

        addButton.onClick.AddListener(ShowInputField);
        inputField.onSubmit.AddListener(OnInputSubmit);
        inputField.onEndEdit.AddListener(OnInputEndEdit);

        inputField.gameObject.SetActive(false);
        warningText.gameObject.SetActive(false);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerDataListChanged -= RefreshUI;
        }
    }

    private void RefreshUI()
    {
        foreach (var btn in currentButtons)
            Destroy(btn);
        currentButtons.Clear();

        List<PlayerData> allPlayers = PlayerDataManager.Instance.GetAllPlayerData();

        if (allPlayers.Count == 0)
        {
            PlayerDataManager.Instance.CreateNewPlayer("New Player");
            return;
        }

        foreach (PlayerData player in allPlayers)
            CreateArchiveButton(player);
    }

    private void CreateArchiveButton(PlayerData player)
    {
        GameObject btnObj = Instantiate(archiveButtonPrefab, buttonContainer);
        currentButtons.Add(btnObj);

        // 设置文本与颜色
        Text textComp = btnObj.GetComponentInChildren<Text>();
        TMP_Text tmpText = btnObj.GetComponentInChildren<TMP_Text>();
        bool isCurrent = PlayerDataManager.Instance.CurrentPlayerData?.PlayerID == player.PlayerID;

        if (textComp != null)
        {
            textComp.text = player.PlayerName;
            if (isCurrent) textComp.color = Color.yellow;
        }
        else if (tmpText != null)
        {
            tmpText.text = player.PlayerName;
            if (isCurrent) tmpText.color = Color.yellow;
        }

        // 主按钮：使用 EventTrigger 处理单击/双击
        Button mainButton = btnObj.GetComponent<Button>();
        if (mainButton != null)
        {
            EventTrigger trigger = btnObj.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btnObj.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => OnButtonClick(data, player.PlayerID));
            trigger.triggers.Add(entry);
        }

        // 删除按钮
        Button deleteButton = FindDeleteButton(btnObj, mainButton);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => OnDeleteClicked(player.PlayerID));
        else
            Debug.LogError("存档按钮预制体中未找到删除按钮！");
    }

    private void OnButtonClick(BaseEventData data, string playerID)
    {
        if (_clickCoroutine != null)
        {
            // 有等待中的协程，说明是双击
            StopCoroutine(_clickCoroutine);
            _clickCoroutine = null;
            OnRenameRequested(playerID);
        }
        else
        {
            // 第一次点击，启动协程等待双击
            _clickCoroutine = StartCoroutine(WaitForDoubleClick(playerID));
        }
    }

    private IEnumerator WaitForDoubleClick(string playerID)
    {
        float timer = 0;
        while (timer < doubleClickTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        // 超时，执行单击
        OnArchiveClicked(playerID);
        _clickCoroutine = null; // 关键修复：协程结束后置空
    }

    private void OnArchiveClicked(string playerID)
    {
        bool success = PlayerDataManager.Instance.LoginWithPlayerID(playerID);
        if (success)
            Debug.Log($"已切换到玩家 {PlayerDataManager.Instance.GetCurrentUsername()}");
        else
            Debug.LogError($"切换玩家失败，ID: {playerID}");
    }

    private void OnRenameRequested(string playerID)
    {
        _renamingPlayerID = playerID;
        _isRenaming = true;
        ShowInputField();
    }

    private void ShowInputField()
    {
        inputField.gameObject.SetActive(true);
        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();
    }

    private void OnInputSubmit(string text)
    {
        HandleInput(text);
    }

    private void OnInputEndEdit(string text)
    {
        // 如果希望失去焦点时也处理，可取消注释
        // HandleInput(text);
    }

    private void HandleInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            inputField.gameObject.SetActive(false);
            _isRenaming = false;
            _renamingPlayerID = null;
            return;
        }

        if (_isRenaming && !string.IsNullOrEmpty(_renamingPlayerID))
        {
            // 重命名模式
            bool success = PlayerDataManager.Instance.RenamePlayer(_renamingPlayerID, text);
            if (success)
                Debug.Log($"玩家重命名为: {text}");
            else
                Debug.LogWarning("重命名失败");
        }
        else
        {
            // 创建新玩家模式
            bool success = PlayerDataManager.Instance.CreateNewPlayer(text);
            if (success)
                Debug.Log($"新玩家 {text} 创建成功！");
            else
                Debug.LogWarning($"创建玩家 {text} 失败，可能用户名已存在？");
        }

        inputField.gameObject.SetActive(false);
        _isRenaming = false;
        _renamingPlayerID = null;
    }

    private void OnDeleteClicked(string playerID)
    {
        if (PlayerDataManager.Instance.GetAllPlayerData().Count <= 1)
        {
            ShowWarning("羁绊之丝若尽，星愿将无法编织。请至少守护一个梦想。", 2f);
            return;
        }

        bool wasCurrent = PlayerDataManager.Instance.CurrentPlayerData?.PlayerID == playerID;
        bool success = PlayerDataManager.Instance.DeletePlayerData(playerID);
        if (success && wasCurrent)
        {
            var allPlayers = PlayerDataManager.Instance.GetAllPlayerData();
            if (allPlayers.Count > 0)
                PlayerDataManager.Instance.LoginWithPlayerID(allPlayers[0].PlayerID);
        }
    }

    private Button FindDeleteButton(GameObject btnObj, Button mainButton)
    {
        Transform deleteTrans = btnObj.transform.Find("DeleteButton");
        if (deleteTrans != null)
            return deleteTrans.GetComponent<Button>();

        Button[] allButtons = btnObj.GetComponentsInChildren<Button>();
        foreach (var btn in allButtons)
        {
            if (btn != mainButton)
                return btn;
        }
        return null;
    }

    private void ShowWarning(string message, float duration = 2f)
    {
        if (_warningCoroutine != null)
            StopCoroutine(_warningCoroutine);
        _warningCoroutine = StartCoroutine(DisplayWarning(message, duration));
    }

    private IEnumerator DisplayWarning(string message, float duration)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        warningText.gameObject.SetActive(false);
        _warningCoroutine = null;
    }
}