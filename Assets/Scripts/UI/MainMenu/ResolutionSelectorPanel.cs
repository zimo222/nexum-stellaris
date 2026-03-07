using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 若使用 TextMeshPro 请保留，否则可删除

/// <summary>
/// 分辨率选择器 UI
/// 挂在包含按钮容器的面板上，自动生成所有可用分辨率的按钮
/// </summary>
public class ResolutionSelectorPanel : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private Transform buttonContainer;   // 按钮的父物体（如 VerticalLayout 的 Content）
    [SerializeField] private GameObject buttonPrefab;      // 按钮预制体（需包含 Button 和 Text / TMP_Text）

    [Header("显示选项")]
    [SerializeField] private bool includeRefreshRate = false; // 按钮上是否显示刷新率

    private void Start()
    {
        GenerateResolutionButtons();
    }

    /// <summary>
    /// 生成分辨率按钮（每次调用会清空容器）
    /// </summary>
    private void GenerateResolutionButtons()
    {
        // 确保 ResolutionManager 存在
        if (ResolutionManager.Instance == null)
        {
            Debug.LogError("ResolutionManager 未找到，请确保场景中有 ResolutionManager 组件！");
            return;
        }

        // 获取唯一分辨率列表
        List<Resolution> resolutions = ResolutionManager.Instance.GetUniqueResolutions();

        // 清空容器中已有的按钮（避免重复生成）
        foreach (Transform child in buttonContainer)
        {
            if (child.name == "ReturnButton") continue;
            Destroy(child.gameObject);
        }

        // 为每个分辨率创建一个按钮
        foreach (Resolution res in resolutions)
        {
            // 实例化按钮
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = buttonObj.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogError("按钮预制体缺少 Button 组件！");
                continue;
            }

            // 构造按钮文本
            string buttonText = includeRefreshRate
                ? $"{res.width} x {res.height}  {res.refreshRate}Hz"
                : $"{res.width} x {res.height}";

            // 尝试设置 TextMeshPro 文本
            TextMeshProUGUI tmpText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = buttonText;
            }
            else
            {
                // 若没有 TMP，则尝试普通 Text
                Text legacyText = buttonObj.GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.text = buttonText;
                }
                else
                {
                    Debug.LogWarning("按钮预制体的子物体中未找到 Text 或 TextMeshProUGUI 组件，无法显示文字。");
                }
            }

            // 绑定点击事件：切换分辨率（保持当前全屏状态）
            btn.onClick.AddListener(() =>
            {
                ResolutionManager.Instance.SetResolution(res.width, res.height, ResolutionManager.Instance.IsFullscreen);
            });
        }
    }

    /// <summary>
    /// 手动刷新按钮列表（例如在语言改变时调用）
    /// </summary>
    public void Refresh()
    {
        GenerateResolutionButtons();
    }
}