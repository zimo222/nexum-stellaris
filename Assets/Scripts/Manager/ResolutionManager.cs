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
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();          // 加载上次保存的设置（首次会设置默认1920x1080非全屏）
            ApplyCurrentSettings();   // 应用加载的设置
        }
        else
        {
            Destroy(gameObject);
        }
        // 注意：删除了原来的 if(IsFullscreen) ToggleFullscreen(); 这行会导致问题
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
    /// 从 PlayerPrefs 加载设置（若不存在则使用默认 1920x1080 非全屏）
    /// </summary>
    private void LoadSettings()
    {
        // 检查是否有保存过分辨率（通过判断键是否存在）
        bool hasSavedResolution = PlayerPrefs.HasKey(resolutionWidthKey) && PlayerPrefs.HasKey(resolutionHeightKey);

        if (!hasSavedResolution)
        {
            // 首次运行：设置为 1920x1080，非全屏，刷新率 60（如果系统支持更高则用系统匹配）
            int defaultWidth = 1920;
            int defaultHeight = 1080;
            bool defaultFullscreen = false;

            // 尝试找到 1920x1080 且刷新率合理的分辨率（尽量取 60Hz 或系统第一个匹配的）
            Resolution defaultRes = Screen.resolutions.FirstOrDefault(r => r.width == defaultWidth && r.height == defaultHeight);
            if (defaultRes.Equals(default(Resolution)))
            {
                // 若系统不支持 1920x1080，则使用当前屏幕分辨率（fallback）
                defaultRes = Screen.currentResolution;
                defaultWidth = defaultRes.width;
                defaultHeight = defaultRes.height;
            }

            CurrentResolution = defaultRes;
            IsFullscreen = defaultFullscreen;
            // 立即保存一次，避免下次启动仍认为首次
            SaveSettings();
        }
        else
        {
            // 已有保存的记录，正常读取
            int width = PlayerPrefs.GetInt(resolutionWidthKey);
            int height = PlayerPrefs.GetInt(resolutionHeightKey);
            bool fullscreen = PlayerPrefs.GetInt(fullscreenKey, 0) == 1;

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
    }

    /// <summary>
    /// 应用当前记录的分辨率与全屏状态
    /// </summary>
    private void ApplyCurrentSettings()
    {
        Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, IsFullscreen, CurrentResolution.refreshRate);
    }
}