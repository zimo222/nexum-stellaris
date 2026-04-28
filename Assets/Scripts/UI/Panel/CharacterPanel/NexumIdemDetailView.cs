using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class NexumIdemDetailView : MonoBehaviour
{
    [Header("通用UI")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeOrPositionText;   // 武器类型或防具部位
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

    private string currentItemDefineId;
    private NexumIdemStarPanelController.NexumIdemMode currentMode;
    private NexumIdemStarPanelController controller;

    public void Initialize(NexumIdemStarPanelController ctrl)
    {
        controller = ctrl;
        equipButton.onClick.AddListener(OnEquipClicked);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(ExotextData weapon)
    {
        if (weapon == null) return;
        currentItemDefineId = weapon.Id;
        currentMode = NexumIdemStarPanelController.NexumIdemMode.Exotext;

        // 填充武器数据
        if (iconImage) iconImage.sprite = GameDataManager.Instance.ExotextDict[weapon.Id].icon;
        if (nameText) nameText.text = LocalizationManager.Instance.GetText("Exotext_Name", weapon.Id) ?? "";
        if (typeOrPositionText) typeOrPositionText.text = weapon.Type.ToString();
        if (starsText) starsText.text = $"★{weapon.Stats.Stars}";
        if (attackText) attackText.text = $"{weapon.Stats.Attack}";
        if (healthText) healthText.text = $"{weapon.Stats.Health}";
        if (defenceText) defenceText.text = $"{weapon.Stats.Defence}";
        if (critRateText) critRateText.text = $"{weapon.Stats.CritRate * 100:F0}%";
        if (critDamageText) critDamageText.text = $"{weapon.Stats.CritDamage * 100:F0}%";
        if (elementBonusText) elementBonusText.text = $"{weapon.Stats.ElementBonus * 100:F0}%";
        if (introductionText) introductionText.text = LocalizationManager.Instance.GetText("Exotext_Introduction", weapon.Id) ?? "";
        //if (descriptionText) descriptionText.text = LocalizationManager.Instance.GetText("Exotext_Description", weapon.Id) ?? "";

        gameObject.SetActive(true);
    }

    public void Show(NexusVestureData vesture)
    {
        if (vesture == null) { Debug.Log("111"); return; }
        currentItemDefineId = vesture.Id;
        currentMode = NexumIdemStarPanelController.NexumIdemMode.NexusVesture;

        if (iconImage) iconImage.sprite = GameDataManager.Instance.NexusVestureDict[vesture.Id].icon;
        if (nameText) nameText.text = LocalizationManager.Instance.GetText("NexusVesture_Name", vesture.Id) ?? "";
        if (typeOrPositionText) typeOrPositionText.text = vesture.Position.ToString();
        if (starsText) starsText.text = $"★{vesture.Stats.Stars}";
        if (attackText) attackText.text = $"{vesture.Stats.Attack}";
        if (healthText) healthText.text = $"{vesture.Stats.Health}";
        if (defenceText) defenceText.text = $"{vesture.Stats.Defence}";
        if (critRateText) critRateText.text = $"{vesture.Stats.CritRate * 100:F0}%";
        if (critDamageText) critDamageText.text = $"{vesture.Stats.CritDamage * 100:F0}%";
        if (elementBonusText) elementBonusText.text = $"{vesture.Stats.ElementBonus * 100:F0}%";
        if (introductionText) introductionText.text = LocalizationManager.Instance.GetText("NexusVesture_Introduction", vesture.Id) ?? "";
        //if (descriptionText) descriptionText.text = LocalizationManager.Instance.GetText("NexusVesture_Description", vesture.Id) ?? "";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnEquipClicked()
    {
        controller?.EquipNexumIdem(currentItemDefineId);
        Hide();
    }
}