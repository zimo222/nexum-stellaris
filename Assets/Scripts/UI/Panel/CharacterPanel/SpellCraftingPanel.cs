using UnityEngine;
using System.Collections.Generic;

public class SpellCraftingPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;
    public Transform slotContainer;
    public Transform libraryContainer;
    public GameObject slotPrefab;
    public GameObject libraryItemPrefab;

    [Header("Module Settings")]
    public int slotCount = 4;                       // 每个武器的模块槽位数

    private List<SpellSlot> slots = new List<SpellSlot>();
    private SpellModuleSO selectedModule;

    // ========== 新增：当前正在编辑的武器索引（0~6） ==========
    private int currentWeaponIndex = 0;

    void Awake()
    {
        // 初始化槽位UI（只做一次）
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            SpellSlot slot = slotObj.GetComponent<SpellSlot>();
            slot.slotIndex = i;
            slot.craftingPanel = this;
            slots.Add(slot);
        }

        // 加载模块库（只做一次）
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
        // 初始加载索引0的配置
        SetCurrentWeaponIndex(0);
    }

    // ========== 外部调用接口：设置当前编辑的武器索引 ==========
    public void SetCurrentWeaponIndex(int index)
    {
        if (index < 0 || index >= 7)
        {
            Debug.LogError($"无效的武器索引: {index}");
            return;
        }
        // 保存当前武器的配置（如果有修改）
        SaveCurrentConfiguration();
        // 切换到新武器
        currentWeaponIndex = index;
        LoadPlayerConfiguration();
    }

    // 从玩家数据加载当前武器的模块列表到槽位
    private void LoadPlayerConfiguration()
    {
        // 假设 GetWeaponModuleList 返回的是 WeaponModuleList 类型
        List<string> moduleIds = PlayerDataManager.Instance.GetWeaponModuleList(currentWeaponIndex);
        if (moduleIds == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < moduleIds.Count && !string.IsNullOrEmpty(moduleIds[i]))
            {
                if (GameDataManager.Instance.SpellModuleDict.TryGetValue(moduleIds[i], out SpellModuleSO module))
                {
                    slots[i].SetModule(module);
                }
                else
                {
                    Debug.LogWarning($"模块ID {moduleIds[i]} 不存在于字典中");
                    slots[i].ClearSlot();
                }
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }

    // 保存当前武器的模块配置到玩家数据
    private void SaveCurrentConfiguration()
    {
        List<string> moduleIds = new List<string>();
        foreach (var slot in slots)
        {
            var module = slot.GetModule();
            moduleIds.Add(module != null ? module.id : "");
        }
        PlayerDataManager.Instance.SaveWeaponModules(currentWeaponIndex, moduleIds);
    }

    // 模块库项点击事件
    public void OnLibraryItemClicked(ModuleLibraryItem item)
    {
        selectedModule = item.module;
        Debug.Log($"选中模块: {selectedModule.moduleName}");
    }

    // 槽位点击事件
    public void OnSlotClicked(SpellSlot slot)
    {
        if (selectedModule != null)
        {
            slot.SetModule(selectedModule);
            selectedModule = null;
            SaveCurrentConfiguration();
        }
        else
        {
            // 如果点击已有模块的槽位且没有选中任何模块，则清除该槽位（可选）
            if (slot.GetModule() != null)
            {
                slot.SetModule(null);
                SaveCurrentConfiguration();
            }
        }
    }
}