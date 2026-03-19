using UnityEngine;
using System.Collections.Generic;

public class SpellCraftingPanel : BPanel
{
    public static SpellCraftingPanel Instance { get; private set; }

    [Header("UI References")]
    public GameObject panelRoot;                  // 整个面板根物体
    public Transform slotContainer;                // 槽位容器
    public Transform libraryContainer;             // 模块库容器
    public GameObject slotPrefab;                   // 槽位预制件
    public GameObject libraryItemPrefab;            // 库项预制件

    public int slotCount = 4;                       // 槽位数量

    private List<SpellSlot> slots = new List<SpellSlot>();
    private SpellModuleSO selectedModule;           // 当前从库中选中的模块（用于放入槽位）

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 初始化槽位
        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            SpellSlot slot = slotObj.GetComponent<SpellSlot>();
            slot.slotIndex = i;
            slots.Add(slot);
        }

        // 加载所有模块库
        foreach (var kv in GameDataManager.Instance.SpellModuleDict)
        {
            GameObject itemObj = Instantiate(libraryItemPrefab, libraryContainer);
            ModuleLibraryItem item = itemObj.GetComponent<ModuleLibraryItem>();
            item.Init(kv.Value);
            item.gameObject.SetActive(true);
        }

        // 初始隐藏面板
        //panelRoot.SetActive(false);
    }

    void Update()
    {
        // 按 Tab 键开关面板（可根据需要改键）
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        UIManager.Instance.OpenPanel("SpellCrafting");
        if (panelRoot.activeSelf)
        {
            // 打开时，从玩家数据加载当前配置到槽位
            LoadPlayerConfiguration();
        }
        else
        {
            // 关闭时，保存当前配置到玩家数据
            SavePlayerConfiguration();
        }
    }

    // 从玩家数据加载已装备的模块到槽位
    private void LoadPlayerConfiguration()
    {
        // 假设 PlayerData 中有一个 List<string> equippedModuleIds 或类似
        List<string> equippedIds = PlayerDataManager.Instance.CurrentPlayerData.equippedModuleIds;
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < equippedIds.Count && !string.IsNullOrEmpty(equippedIds[i]))
            {
                SpellModuleSO module = GameDataManager.Instance.SpellModuleDict[equippedIds[i]];
                slots[i].SetModule(module);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }

    private void SavePlayerConfiguration()
    {
        List<string> equippedIds = new List<string>();
        foreach (var slot in slots)
        {
            var module = slot.GetModule();
            equippedIds.Add(module != null ? module.id : "");
        }
        PlayerDataManager.Instance.CurrentPlayerData.equippedModuleIds = equippedIds;
        PlayerDataManager.Instance.SaveCurrentPlayerData();
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
            // 将选中的模块放入槽位
            slot.SetModule(selectedModule);
            selectedModule = null; // 清空选中

            SavePlayerConfiguration();
        }
        else
        {
            // 如果没有选中模块，可以右键清除等，先简单处理：点击已有模块的槽位，可以移除（可选）
            // 这里实现双击或右键移除，暂时不做
        }
    }
}