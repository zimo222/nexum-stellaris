using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponDetailView : MonoBehaviour
{
    [Header("UI 组件")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponTypeText;
    public TextMeshProUGUI starsText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI defenceText;
    public TextMeshProUGUI critRateText;
    public TextMeshProUGUI critDamageText;
    public TextMeshProUGUI elementBonusText;
    public TextMeshProUGUI introductionText;
    public TextMeshProUGUI descriptionText;
    public Button equipButton;
    public Button closeButton;

    private string currentWeaponDefineId;
    private WeaponStarPanelController controller;

    public void Initialize(WeaponStarPanelController ctrl)
    {
        controller = ctrl;
        equipButton.onClick.AddListener(OnEquipClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(ExotextData weapon)
    {
        if (weapon == null) return;
        currentWeaponDefineId = weapon.Id;

        ExotextDefineSO currentExotext = GameDataManager.Instance.ExotextDict[weapon.Id];

        // 填充数据（根据实际字段调整）
        if (weaponNameText) weaponNameText.text = LocalizationManager.Instance.GetText("Exotext_Name", weapon.Id + "_Name") ?? "";
        if (weaponTypeText) weaponTypeText.text = weapon.Type.ToString();
        if (starsText) starsText.text = $"★{weapon.Stats.Stars}";
        if (attackText) attackText.text = $"攻击 {weapon.Stats.Attack}";
        if (healthText) healthText.text = $"生命 {weapon.Stats.Health}";
        if (defenceText) defenceText.text = $"防御 {weapon.Stats.Defence}";
        if (critRateText) critRateText.text = $"暴击率 {weapon.Stats.CritRate * 100:F0}%";
        if (critDamageText) critDamageText.text = $"暴击伤害 {weapon.Stats.CritDamage * 100:F0}%";
        if (elementBonusText) elementBonusText.text = $"元素加成 {weapon.Stats.ElementBonus * 100:F0}%";
        if (introductionText) introductionText.text = LocalizationManager.Instance.GetText("Exotext_Introduction", weapon.Id) ?? "";
        if (descriptionText) descriptionText.text = currentExotext.description ?? "";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnEquipClicked()
    {
        controller?.EquipWeapon(currentWeaponDefineId);
        Hide(); // 装备后关闭详情
    }
}