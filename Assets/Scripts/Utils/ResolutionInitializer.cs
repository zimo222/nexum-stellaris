using UnityEngine;

/// <summary>
/// 游戏启动时强制设置分辨率为1920x1080
/// </summary>
public class ResolutionInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SetInitialResolution()
    {
        // 如果当前分辨率已经是1920x1080，则不做任何操作（可选优化）
        if (Screen.width == 1920 && Screen.height == 1080)
            return;

        // 设置分辨率：宽1920，高1080，使用全屏窗口模式（无边框窗口）
        // 若需要独占全屏，可将FullScreenMode改为FullScreenMode.ExclusiveFullScreen
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

        // 可选：输出日志便于调试
        Debug.Log("分辨率已强制设置为：1920x1080，全屏模式：" + Screen.fullScreenMode);
    }
}