using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class WeaponStarView : MonoBehaviour
{
    [Header("配置")]
    public string weaponDefineId;          // 对应武器的 ID
    public Image starImage;                // 星辰图片（用于变色）
    public TextMeshProUGUI starNameText;   // 可选：显示武器名

    [Header("状态颜色")]
    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);       // 未解锁
    public Color unlockedNotEquippedColor = new Color(0.8f, 0.6f, 0.2f, 1f); // 暗黄
    public Color equippedColor = new Color(1, 1, 0, 1);   // 亮金

    private Button button;
    private WeaponStarPanelController controller;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Initialize(WeaponStarPanelController ctrl)
    {
        controller = ctrl;
    }

    /// <summary> 根据武器状态更新颜色 </summary>
    public void UpdateState(bool unlocked, bool isEquipped)
    {
        if (starImage == null) return;
        if (!unlocked)
            starImage.color = lockedColor;
        else if (isEquipped)
            starImage.color = equippedColor;
        else
            starImage.color = unlockedNotEquippedColor;
    }

    private void OnClick()
    {
        controller?.OnStarClicked(this);
    }
}