using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotsUI : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private Image[] backImages; // 在 Inspector 中按顺序拖入 7 个 BackImage
    [SerializeField] private Image[] iconImages; // 在 Inspector 中按顺序拖入 7 个 BackImage

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private int currentIndex = -1;

    private void Start()
    {
        PlayerDataManager.Instance.OnPlayerDataChanged += RefreshWeaponIcon;
        // 初始默认选中第一个（或根据玩家当前武器）
        SelectSlot(0);
        RefreshWeaponIcon(PlayerDataManager.Instance.CurrentPlayerData);
    }

    private void OnDestroy()
    {
        PlayerDataManager.Instance.OnPlayerDataChanged -= RefreshWeaponIcon;
    }

    /// <summary>
    /// 切换选中的武器槽位（0 ~ 6 对应 1~7）
    /// </summary>
    public void SelectSlot(int index)
    {
        if (backImages == null || backImages.Length == 0)
        {
            Debug.LogWarning("WeaponSlotsUI: backImages 数组未赋值！");
            return;
        }

        // 范围限制
        index = Mathf.Clamp(index, 0, backImages.Length - 1);

        // 如果与当前相同则不重复处理（可选）
        if (currentIndex == index) return;

        // 将上一个选中的恢复默认颜色
        if (currentIndex >= 0 && currentIndex < backImages.Length)
            backImages[currentIndex].color = normalColor;

        // 设置新选中的高亮颜色
        backImages[index].color = selectedColor;
        currentIndex = index;
    }

    public void RefreshWeaponIcon(PlayerData playerData)
    {
        int i = 0;
        foreach(Image iconImage in iconImages)
        {
            if (GameDataManager.Instance.ExotextDict.TryGetValue(playerData.EquippedExotextIds[i], out var value))
            {
                iconImage.sprite = value.icon;
            }
            else
            {
                iconImage.color = new Color(1, 1, 1, 0);
            }
            i++;
        }
    }

    /// <summary>
    /// 供外部获取当前选中索引
    /// </summary>
    public int CurrentIndex => currentIndex;
}