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

    // ========== 新增：模块设计面板引用 ==========
    [Header("模块设计")]
    [SerializeField] private SpellCraftingPanel spellCraftingPanel;

    private PlayerData playerData;
    private GameObject currentPanel;
    private Button[] weaponButtons;          // 缓存武器按钮数组

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        LoadData();
        InitializeUI();
        BindWeaponButtons();   // 绑定武器按钮事件
    }

    void LoadData()
    {
        if (PlayerDataManager.Instance != null)
            playerData = PlayerDataManager.Instance.CurrentPlayerData;
        else
            playerData = new PlayerData("测试玩家");
    }

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
        }

        view.UpdateUI(playerData);
    }

    // ========== 绑定武器选择按钮（使用 menuSections[2].smallButtons） ==========
    private void BindWeaponButtons()
    {
        // 确保存在第3个菜单栏（索引2），且其 smallButtons 长度至少为7
        if (menuSections.Length <= 2)
        {
            Debug.LogError("menuSections 中没有索引为2的菜单栏，无法获取武器按钮");
            return;
        }

        weaponButtons = menuSections[2].smallButtons;
        if (weaponButtons == null || weaponButtons.Length < 7)
        {
            Debug.LogError("武器按钮数量不足7个，请检查 menuSections[2].smallButtons 配置");
            return;
        }

        for (int i = 0; i < weaponButtons.Length; i++)
        {
            int weaponIndex = i;
            weaponButtons[i].onClick.AddListener(() => OnWeaponButtonClicked(weaponIndex));
        }

        // 默认选中第一个武器按钮的高亮（可选）
        HighlightWeaponButton(0);
    }

    // 武器按钮点击回调
    private void OnWeaponButtonClicked(int weaponIndex)
    {
        if (spellCraftingPanel == null)
        {
            Debug.LogError("未引用 SpellCraftingPanel");
            return;
        }
        spellCraftingPanel.SetCurrentWeaponIndex(weaponIndex);
        HighlightWeaponButton(weaponIndex);
        Debug.Log($"切换到武器 {weaponIndex} 的模块配置");
    }

    // 高亮当前选中的武器按钮（复用 small 高亮颜色）
    private void HighlightWeaponButton(int selectedIndex)
    {
        if (weaponButtons == null) return;
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            Button btn = weaponButtons[i];
            if (btn != null && btn.targetGraphic != null)
            {
                btn.targetGraphic.color = (i == selectedIndex) ? smallSelectedColor : smallNormalColor;
            }
        }
    }

    private void OnLargeButtonClick(int LargeIndex)
    {
        if (largeIndex == LargeIndex) return;
        largeIndex = LargeIndex;
        smallIndex = -1;

        for (int i = 0; i < menuSections.Length; i++)
        {
            bool isCurrent = (i == largeIndex);
            SetLargeButtonAppearance(i, isCurrent);
            SetSmallButtonsActive(i, isCurrent);
        }

        if (menuSections[largeIndex].smallButtons.Length > 0)
        {
            OnSmallButtonClick(largeIndex, 0);
        }
        else
        {
            if (currentPanel != null) currentPanel.gameObject.SetActive(false);
            currentPanel = menuSections[largeIndex].largePanel;
            currentPanel.gameObject.SetActive(true);
        }
    }

    private void OnSmallButtonClick(int largeIdx, int smallIdx)
    {
        if (smallIndex == smallIdx) return;
        if (currentPanel != null) currentPanel.gameObject.SetActive(false);
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

        // ========== 如果切换到的是模块设计面板，刷新当前武器的显示 ==========
        // 假设模块设计面板对应的 largeIdx=2，且它的某个 smallPanel 就是 SpellCraftingPanel 所在的面板。
        // 为了确保显示正确的武器数据，可以主动调用一次设置当前武器索引（保持当前选中的武器）
        if (largeIdx == 2 && spellCraftingPanel != null)
        {
            // 获取当前选中的武器索引（默认0，或者从高亮按钮中获取）
            int currentWeapon = GetCurrentWeaponIndex();
            spellCraftingPanel.SetCurrentWeaponIndex(currentWeapon, false);
            HighlightWeaponButton(currentWeapon);
        }
    }

    // 辅助方法：获取当前选中的武器索引（根据高亮按钮颜色判断）
    private int GetCurrentWeaponIndex()
    {
        if (weaponButtons == null) return 0;
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            if (weaponButtons[i].targetGraphic != null && weaponButtons[i].targetGraphic.color == smallSelectedColor)
                return i;
        }
        return 0; // 默认第一个
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