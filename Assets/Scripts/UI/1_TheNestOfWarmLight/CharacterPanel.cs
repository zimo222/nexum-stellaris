using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CharacterPanel : BPanel
{
    [Header("选项卡")]
    [SerializeField] private Button[] tabButtons;          // 角色、装备、技能按钮
    [SerializeField] private GameObject[] tabContents;     // 对应的内容区域

    [Header("角色信息")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenceText;
    [SerializeField] private TextMeshProUGUI critRateText;
    [SerializeField] private TextMeshProUGUI critDamageText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI statsText;    // 基础属性

    [Header("装备区域")]
    [SerializeField] private Transform equipmentContainer; // GridLayoutGroup的父物体
    [SerializeField] private GameObject equipmentSlotPrefab;
    [SerializeField] private EquipmentDetailPanel detailPanel;

    private List<EquipmentSlot> spawnedSlots = new List<EquipmentSlot>();
    private PlayerData currentPlayerData;

    private void Start()
    {
        // 绑定选项卡点击事件
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => SwitchTab(index));
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        RefreshData();
        SwitchTab(0); // 默认选中角色
    }

    private void RefreshData()
    {
        currentPlayerData = PlayerDataManager.Instance.CurrentPlayerData;
        if (currentPlayerData == null) return;

        UpdateCharacterInfo();
        RefreshEquipmentList();
    }

    private void UpdateCharacterInfo()
    {
        if(nameText != null) nameText.text = currentPlayerData.PlayerName;
        if(levelText != null) levelText.text = $"Lv.{currentPlayerData.Level}";
        if(expText != null) expText.text = $"EXP: {currentPlayerData.Experience}";

        var stats = currentPlayerData.BaseStats;
        if (healthText != null) healthText.text = $"{stats.Health}";
        if (attackText != null) attackText.text = $"{stats.Attack}";
        if (defenceText != null) defenceText.text = $"{stats.Defence}";
        if (critRateText != null) critRateText.text = $"{stats.CritRate * 100}%";
        if (critDamageText != null) critDamageText.text = $"{stats.CritDamage * 100}%";
        if (energyText != null) energyText.text = $"{stats.Energy}%";

    }

    private void RefreshEquipmentList()
    {
        // 清除旧槽位
        foreach (var slot in spawnedSlots)
            Destroy(slot.gameObject);
        spawnedSlots.Clear();

        // 合并装备列表（绎语 + 络身）
        List<NexumIdemData> allEquipment = new List<NexumIdemData>();
        allEquipment.AddRange(currentPlayerData.ExotextBag);
        allEquipment.AddRange(currentPlayerData.NexusVestureBag);

        // 生成新槽位
        foreach (var equip in allEquipment)
        {
            bool isEquipped = IsEquipped(equip);
            GameObject slotObj = Instantiate(equipmentSlotPrefab, equipmentContainer);
            var slot = slotObj.GetComponent<EquipmentSlot>();
            slot.Init(equip, isEquipped, OnSlotPointerEnter, OnSlotPointerExit);
            spawnedSlots.Add(slot);
        }
    }

    private bool IsEquipped(NexumIdemData equip)
    {
        if (equip is ExotextData)
        {
            return currentPlayerData.EquippedExotextIndex >= 0 &&
                   currentPlayerData.ExotextBag.Count > currentPlayerData.EquippedExotextIndex &&
                   currentPlayerData.ExotextBag[currentPlayerData.EquippedExotextIndex] == equip;
        }
        else if (equip is NexusVestureData vesture)
        {
            int index = currentPlayerData.NexusVestureBag.IndexOf(vesture);
            if (index == -1) return false;
            return index == currentPlayerData.EquippedCogniThreadIndex ||
                   index == currentPlayerData.EquippedTangibleNexusIndex ||
                   index == currentPlayerData.EquippedAbyssalHeartIndex ||
                   index == currentPlayerData.EquippedVolitionVeinIndex ||
                   index == currentPlayerData.EquippedImprintStepIndex;
        }
        return false;
    }

    private void OnSlotPointerEnter(NexumIdemData data)
    {
        detailPanel.Show(data);
    }

    private void OnSlotPointerExit()
    {
        detailPanel.Hide();
    }

    private void SwitchTab(int index)
    {
        for (int i = 0; i < tabContents.Length; i++)
            tabContents[i].SetActive(i == index);
    }
}