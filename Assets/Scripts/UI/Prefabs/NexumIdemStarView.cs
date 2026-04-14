using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal; // 如果使用 URP 2D 灯光

[RequireComponent(typeof(Button))]
public class NexumIdemStarView : MonoBehaviour
{
    [Header("配置")]
    public string ItemDefineId;              // 装备ID（武器或防具）
    public Image starImage;
    public TextMeshProUGUI starNameText;

    private Light2D myLight;      // 2D 灯光 (URP)

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

        // 获取子对象 "MyLight" 上的 Light2D 组件
        Transform lightTransform = transform.Find("Light 2D");
        if (lightTransform != null)
        {
            myLight = lightTransform.GetComponent<Light2D>();
            // 如果是普通 Light 组件: myLight = lightTransform.GetComponent<Light>();
        }

        // 控制启用/禁用
        myLight.enabled = false;   // 禁用灯光
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
        {
            starImage.color = lockedColor;
            myLight.enabled = false;
        }
        else if (isEquipped)
        {
            starImage.color = equippedColor;
            myLight.enabled = true;
            myLight.intensity = 0.5f;   // 设置强度为 1.5
        }
        else
        {
            starImage.color = unlockedNotEquippedColor;
            myLight.enabled = true;
            myLight.intensity = 0.05f;   // 设置强度为 1.5
        }
    }

    private void OnClick()
    {
        controller?.OnStarClicked(this);
    }
}