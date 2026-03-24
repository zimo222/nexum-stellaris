using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentDetailPanel : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI starText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI introText;
    [SerializeField] private GameObject panel; // 整个面板，用于显示/隐藏

    private void Awake()
    {
        Hide();
    }

    public void Show(NexumIdemData data)
    {
        if (data == null) return;

        // 图标
        iconImage.sprite = GetIcon(data);

        // 名称
        //nameText.text = data.Name;

        // 星级
        starText.text = $"星级: {data.Stats.Stars}";

        // 属性
        var s = data.Stats;
        string stats = $"生命: {s.Health}\n攻击: {s.Attack}\n防御: {s.Defence}\n能量: {s.Energy}\n暴击率: {s.CritRate:P1}\n暴击伤害: {s.CritDamage:P1}\n元素加成: {s.ElementBonus:P1}";
        statsText.text = stats;

        // 介绍与描述
        //introText.text = $"{data.TextStats.Introduction}\n{data.TextStats.Description}";

        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private Sprite GetIcon(NexumIdemData data)
    {
        if (data is ExotextData)
        {
            if (GameDataManager.Instance.ExotextDict.TryGetValue(data.Id, out var def))
                return def.icon;
        }
        else if (data is NexusVestureData)
        {
            if (GameDataManager.Instance.NexusVestureDict.TryGetValue(data.Id, out var def))
                return def.icon;
        }
        return null;
    }
}