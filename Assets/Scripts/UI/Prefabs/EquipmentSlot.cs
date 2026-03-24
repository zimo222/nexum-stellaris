using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class EquipmentSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI starText;
    [SerializeField] private GameObject equippedMark;

    private NexumIdemData equipmentData;
    private Action<NexumIdemData> onPointerEnter;
    private Action onPointerExit;

    public void Init(NexumIdemData data, bool isEquipped, Action<NexumIdemData> enterCallback, Action exitCallback)
    {
        equipmentData = data;
        onPointerEnter = enterCallback;
        onPointerExit = exitCallback;

        // 设置图标
        iconImage.sprite = GetIcon(data);

        // 设置星级显示
        starText.text = $"★{data.Stats.Stars}";

        // 装备标记
        if (equippedMark != null)
            equippedMark.SetActive(isEquipped);
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter?.Invoke(equipmentData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onPointerExit?.Invoke();
    }
}