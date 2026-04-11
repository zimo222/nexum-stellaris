using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;   // 如果使用TextMeshPro

public class SpellCraftingPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;
    public Transform slotContainer;
    public Transform libraryContainer;
    public GameObject slotPrefab;
    public GameObject libraryItemPrefab;

    [Header("Module Settings")]
    public int slotCount = 4;

    [Header("Detail Panel")]   // 新增：详情面板
    public GameObject detailPanel;                // 整个详情面板的根对象
    public Image detailSpellIcon;
    public TMP_Text detailModuleName;             // 模块名称
    public TMP_Text detailModuleType;             // 模块类型
    public TMP_Text IntroductionText;

    private List<SpellSlot> slots = new List<SpellSlot>();
    private SpellModuleSO selectedModule;
    private ModuleLibraryItem currentSelectedLibraryItem;   // 当前选中的库项
    private int currentWeaponIndex = 0;

    void Awake()
    {
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            SpellSlot slot = slotObj.GetComponent<SpellSlot>();
            slot.slotIndex = i;
            slot.craftingPanel = this;
            slots.Add(slot);
        }

        foreach (var kv in GameDataManager.Instance.SpellModuleDict)
        {
            GameObject itemObj = Instantiate(libraryItemPrefab, libraryContainer);
            ModuleLibraryItem item = itemObj.GetComponent<ModuleLibraryItem>();
            item.craftingPanel = this;
            item.Init(kv.Value);
            item.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        SetCurrentWeaponIndex(0, false);
        // 初始时详情面板不可见
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    public void SetCurrentWeaponIndex(int index, bool saveCurrent = true)
    {
        if (index < 0 || index >= 7)
        {
            Debug.LogError($"无效的武器索引: {index}");
            return;
        }

        if (saveCurrent)
            SaveCurrentConfiguration();

        currentWeaponIndex = index;
        LoadPlayerConfiguration();
        ClearModuleSelection();
    }

    private void LoadPlayerConfiguration()
    {
        List<string> moduleIds = PlayerDataManager.Instance.GetWeaponModuleList(currentWeaponIndex);
        if (moduleIds == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < moduleIds.Count && !string.IsNullOrEmpty(moduleIds[i]))
            {
                if (GameDataManager.Instance.SpellModuleDict.TryGetValue(moduleIds[i], out SpellModuleSO module))
                    slots[i].SetModule(module);
                else
                {
                    Debug.LogWarning($"模块ID {moduleIds[i]} 不存在");
                    slots[i].ClearSlot();
                }
            }
            else
                slots[i].ClearSlot();
        }
    }

    private void SaveCurrentConfiguration()
    {
        List<string> moduleIds = new List<string>();
        foreach (var slot in slots)
            moduleIds.Add(slot.GetModule() != null ? slot.GetModule().id : "");
        PlayerDataManager.Instance.SaveWeaponModules(currentWeaponIndex, moduleIds);
    }

    // 模块库项点击
    public void OnLibraryItemClicked(ModuleLibraryItem item)
    {
        // 如果点击的是同一个模块 → 视为取消选中
        if (currentSelectedLibraryItem == item)
        {
            ClearModuleSelection();
            return;
        }

        // 清除之前的选中高亮
        if (currentSelectedLibraryItem != null)
            currentSelectedLibraryItem.SetHighlight(false);

        // 设置新的选中
        currentSelectedLibraryItem = item;
        currentSelectedLibraryItem.SetHighlight(true);
        selectedModule = item.module;

        // 更新详情面板
        UpdateDetailPanel(selectedModule);
    }

    // 槽位点击（放置或清除）
    public void OnSlotClicked(SpellSlot slot)
    {
        if (selectedModule != null)
        {
            // 有选中模块 → 放入槽位
            slot.SetModule(selectedModule);
            SaveCurrentConfiguration();
            // 放入后清除选中状态
            ClearModuleSelection();
        }
        else
        {
            // 没有选中模块 → 如果槽位有模块则清除
            if (slot.GetModule() != null)
            {
                slot.SetModule(null);
                SaveCurrentConfiguration();
                // 不清除选中，因为没有选中任何模块
            }
        }
    }

    // 清除当前选中的模块、高亮和详情面板
    private void ClearModuleSelection()
    {
        if (currentSelectedLibraryItem != null)
        {
            currentSelectedLibraryItem.SetHighlight(false);
            currentSelectedLibraryItem = null;
        }
        selectedModule = null;
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    // 更新详情面板显示内容
    private void UpdateDetailPanel(SpellModuleSO module)
    {
        if (module == null)
        {
            if (detailPanel != null) detailPanel.SetActive(false);
            return;
        }

        if (detailPanel != null) detailPanel.SetActive(true);

        // 通用字段
        if (detailSpellIcon != null) detailSpellIcon.sprite = module.icon;
        if (detailModuleName != null) detailModuleName.text = module.moduleName;
        if (detailModuleType != null) detailModuleType.text = GetTypeText(module.moduleType.ToString());
        if (detailModuleType != null) detailModuleType.text = GetTypeText(module.moduleType.ToString());
        if (IntroductionText != null) IntroductionText.text = module.introduction;
    }

    private string GetTypeText(string type)
    {
        switch(type)
        {
            case "Projectile":
                return "投射";
            case "Modifier":
                return "修饰";
            case "Corrector":
                return "修正";
            case "MultiCast":
                return "多重释放";
            default:
                return null;
        }
    }
}