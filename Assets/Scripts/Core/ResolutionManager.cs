using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 分辨率管理器（单例）
/// 负责获取可用分辨率、切换分辨率与全屏状态、保存/加载设置
/// </summary>
public class ResolutionManager : MonoBehaviour
{
    // 单例实例
    public static ResolutionManager Instance { get; private set; }

    [Header("设置保存键名")]
    [SerializeField] private string resolutionWidthKey = "ResolutionWidth";
    [SerializeField] private string resolutionHeightKey = "ResolutionHeight";
    [SerializeField] private string fullscreenKey = "Fullscreen";

    // 当前分辨率（实际应用的分辨率）
    public Resolution CurrentResolution { get; private set; }
    // 当前是否全屏
    public bool IsFullscreen { get; private set; }

    private void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();          // 加载上次保存的设置
            ApplyCurrentSettings();   // 应用加载的设置
        }
        else
        {
            Destroy(gameObject);
        }
        ToggleFullscreen();
    }

    /// <summary>
    /// 获取所有支持的分辨率（过滤掉重复宽高，只保留每种分辨率的一个代表）
    /// </summary>
    public List<Resolution> GetUniqueResolutions()
    {
        // Screen.resolutions 返回系统支持的所有分辨率（包含不同刷新率）
        Resolution[] all = Screen.resolutions;

        // 按宽高分组，取每组第一个（通常刷新率较高的会被列在前面）
        var unique = all
            .GroupBy(res => new { res.width, res.height })
            .Select(g => g.First())
            .ToList();

        return unique;
    }

    /// <summary>
    /// 设置分辨率并切换全屏状态
    /// </summary>
    /// <param name="width">宽</param>
    /// <param name="height">高</param>
    /// <param name="fullscreen">是否全屏</param>
    /// <param name="preferredRefreshRate">期望刷新率（0表示使用系统当前默认）</param>
    public void SetResolution(int width, int height, bool fullscreen, int preferredRefreshRate = 0)
    {
        // 查找匹配的分辨率对象，以便获取刷新率
        Resolution target = new Resolution { width = width, height = height };
        Resolution[] all = Screen.resolutions;

        // 尝试找到用户指定的分辨率（包含宽高和刷新率）
        Resolution matched = all.FirstOrDefault(res =>
            res.width == width && res.height == height &&
            (preferredRefreshRate == 0 || res.refreshRate == preferredRefreshRate));

        // 如果没找到精确匹配，则找一个宽高相同的任意分辨率
        if (matched.Equals(default(Resolution)))
        {
            matched = all.FirstOrDefault(res => res.width == width && res.height == height);
        }

        // 如果依然没找到（理论上不可能，但以防万一），用参数构建
        if (matched.Equals(default(Resolution)))
        {
            matched.width = width;
            matched.height = height;
            matched.refreshRate = preferredRefreshRate > 0 ? preferredRefreshRate : 60;
        }

        // 应用分辨率
        Screen.SetResolution(matched.width, matched.height, fullscreen, matched.refreshRate);

        // 更新当前记录
        CurrentResolution = matched;
        IsFullscreen = fullscreen;

        Debug.Log($"分辨率已设置为: {width}x{height} {matched.refreshRate}Hz 全屏={fullscreen}");

        // 保存设置
        SaveSettings();
    }

    /// <summary>
    /// 切换全屏模式（保留当前分辨率）
    /// </summary>
    public void ToggleFullscreen()
    {
        SetResolution(CurrentResolution.width, CurrentResolution.height, !IsFullscreen, CurrentResolution.refreshRate);
    }

    /// <summary>
    /// 保存当前分辨率与全屏状态到 PlayerPrefs
    /// </summary>
    private void SaveSettings()
    {
        PlayerPrefs.SetInt(resolutionWidthKey, CurrentResolution.width);
        PlayerPrefs.SetInt(resolutionHeightKey, CurrentResolution.height);
        PlayerPrefs.SetInt(fullscreenKey, IsFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从 PlayerPrefs 加载设置（若不存在则使用当前屏幕设置）
    /// </summary>
    private void LoadSettings()
    {
        int width = PlayerPrefs.GetInt(resolutionWidthKey, Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt(resolutionHeightKey, Screen.currentResolution.height);
        bool fullscreen = PlayerPrefs.GetInt(fullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        // 查找对应的分辨率对象（填充刷新率）
        Resolution res = new Resolution { width = width, height = height };
        Resolution matched = Screen.resolutions.FirstOrDefault(r => r.width == width && r.height == height);
        if (matched.Equals(default(Resolution)))
        {
            // 保存的分辨率不再支持？则使用当前屏幕分辨率
            res = Screen.currentResolution;
        }
        else
        {
            res.refreshRate = matched.refreshRate;
        }

        CurrentResolution = res;
        IsFullscreen = fullscreen;
    }

    /// <summary>
    /// 应用当前记录的分辨率与全屏状态
    /// </summary>
    private void ApplyCurrentSettings()
    {
        Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, IsFullscreen, CurrentResolution.refreshRate);
    }
}