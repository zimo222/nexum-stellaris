using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class NexumIdemStarView : MonoBehaviour
{
    [Header("配置")]
    public string ItemDefineId;              // 装备ID（武器或防具）
    public Image starImage;
    public TextMeshProUGUI starNameText;

    [Header("状态颜色")]
    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color unlockedNotEquippedColor = new Color(0.8f, 0.6f, 0.2f, 1f);
    public Color equippedColor = new Color(1, 1, 0, 1);

    public NexumIdemStarPanelController.NexumIdemMode Mode { get; private set; }

    private Button button;
    private NexumIdemStarPanelController controller;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void Initialize(NexumIdemStarPanelController ctrl, NexumIdemStarPanelController.NexumIdemMode mode)
    {
        controller = ctrl;
        Mode = mode;
    }

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