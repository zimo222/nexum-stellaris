using System.Collections.Generic;
using TMPro; // 若使用 TextMeshPro 请保留，否则可删除using UnityEngine.Localization.Settings;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 语言选择器 UI
/// 挂在包含按钮容器的面板上，自动生成所有可用分辨率的按钮
/// </summary>
public class LanguageSelectorPanel : MonoBehaviour
{
    [Header("UI 组件")]
    [SerializeField] private Button[] button;  

    private void Start()
    {
        button[0].onClick.AddListener(() => TransformLanguage("en"));
        button[1].onClick.AddListener(() => TransformLanguage("zh-Hans"));
    }

    void TransformLanguage(string language)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(language);
    }
}