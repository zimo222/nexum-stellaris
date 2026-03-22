using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PanelMenuSection
{
    public Button largeButton;
    public GameObject largePanel;
    public Button[] smallButtons;
    public GameObject[] smallPanel;
}
public class CharacterPanelController : BPanel
{
    [Header("View引用")]
    [SerializeField] private CharacterPanelView view;

    [Header("菜单栏")]
    [SerializeField] private PanelMenuSection[] menuSections;
    private int largeIndex = -1, smallIndex = -1;


    [Header("颜色方案")]
    [SerializeField] private Color largeSelectedColor = Color.white;
    [SerializeField] private Color largeNormalColor = Color.gray;
    [SerializeField] private Color smallSelectedColor = Color.yellow;
    [SerializeField] private Color smallNormalColor = Color.white;

    private PlayerData playerData;
    private GameObject currentPanel;

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 初始化
    void Initialize()
    {
        LoadData();
        InitializeUI();
    }

    // 加载玩家数据
    void LoadData()
    {
        if (PlayerDataManager.Instance != null)
            playerData = PlayerDataManager.Instance.CurrentPlayerData;
        else
            playerData = new PlayerData("测试玩家");
    }

    // 初始化UI
    void InitializeUI()
    {
        for (int i = 0; i < menuSections.Length; i++)
        {
            int largeIdx = i;
            PanelMenuSection section = menuSections[i];
            section.largeButton.onClick.AddListener(() => OnLargeButtonClick(largeIdx));
            for (int j = 0; j < section.smallButtons.Length; j++)
            {
                int smallIdx = j;
                section.smallButtons[j].onClick.AddListener(() => OnSmallButtonClick(largeIdx, smallIdx));
            }
        }

        if (menuSections.Length > 0)
        {
            OnLargeButtonClick(0);
            /*
            if (menuSections[0].smallButtonPools != null && menuSections[0].smallButtonPools.Length > 0)
            {
                var firstPool = menuSections[0].smallButtonPools[0];
            }
            */
        }
        /*
        // 绑定详情面板关闭按钮（仅用于材料）
        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(HideDetailPanel);

        // 初始化详情面板为隐藏
        if (detailPanel != null)
            detailPanel.SetActive(false);

        */
        view.UpdateUI(playerData);
    }

    private void OnLargeButtonClick(int LargeIndex)
    {
        if (largeIndex == LargeIndex) return;//点的是当前的
        largeIndex = LargeIndex;
        smallIndex = -1;

        for (int i = 0; i < menuSections.Length; i++)
        {
            bool isCurrent = (i == largeIndex);
            SetLargeButtonAppearance(i, isCurrent);
            SetSmallButtonsActive(i, isCurrent);
        }

        if (menuSections[largeIndex].smallButtons.Length > 0)//有小面板
            OnSmallButtonClick(largeIndex, 0);
        else//没小面板
        {
            if(currentPanel != null) currentPanel.gameObject.SetActive(false);
            currentPanel = menuSections[largeIndex].largePanel;
            currentPanel.gameObject.SetActive(true);
        }
    }

    private void OnSmallButtonClick(int largeIdx, int smallIdx)
    {
        currentPanel.gameObject.SetActive(false);

        if (smallIndex == smallIdx) return;
        smallIndex = smallIdx;
        SetSmallButtonHighlight(largeIdx, smallIdx);

        // 检查数组和元素
        if (menuSections[largeIdx].smallPanel == null)
        {
            Debug.LogError($"smallPanel 数组为 null，largeIdx={largeIdx}");
            return;
        }
        if (menuSections[largeIdx].smallPanel.Length <= smallIdx)
        {
            Debug.LogError($"smallPanel 数组长度不足：长度={menuSections[largeIdx].smallPanel.Length}，索引={smallIdx}");
            return;
        }
        currentPanel = menuSections[largeIdx].smallPanel[smallIdx];
        currentPanel.gameObject.SetActive(true);
    }

    private void SetLargeButtonAppearance(int largeIdx, bool isSelected)
    {
        Button btn = menuSections[largeIdx].largeButton;
        if (btn.targetGraphic != null)
        {
            btn.targetGraphic.color = isSelected ? largeSelectedColor : largeNormalColor;

        }
    }

    private void SetSmallButtonsActive(int largeIdx, bool active)
    {
        foreach (Button btn in menuSections[largeIdx].smallButtons)
            btn.gameObject.SetActive(active);
    }

    private void SetSmallButtonHighlight(int largeIdx, int smallIdx)
    {
        PanelMenuSection section = menuSections[largeIdx];
        for (int i = 0; i < section.smallButtons.Length; i++)
        {
            Button btn = section.smallButtons[i];
            if (btn.targetGraphic != null)
            {
                btn.targetGraphic.color = (i == smallIdx) ? smallSelectedColor : smallNormalColor;
            }
        }
    }

}
