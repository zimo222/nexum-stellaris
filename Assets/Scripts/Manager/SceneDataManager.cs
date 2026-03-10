using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneDataManager : Singleton<SceneDataManager>
{
    [Header("Loading Settings")]
    [SerializeField] private LoadingPanel loadingPanel;  // 拖入加载面板
    [SerializeField] private float minimumLoadTime = 1f; // 最短加载时间
    [SerializeField] private float fadeInDuration = 0.3f;  // 淡入时间
    [SerializeField] private float fadeOutDuration = 0.3f; // 淡出时间

    private bool isLoading = false;
    private CanvasGroup canvasGroup; // 用于控制透明度

    protected override void Awake()
    {
        base.Awake();

        if (loadingPanel != null)
        {
            loadingPanel.gameObject.SetActive(false);
            // 确保 CanvasGroup 组件存在
            canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = loadingPanel.gameObject.AddComponent<CanvasGroup>();

            // 将加载面板所在的根 Canvas 设为常驻，避免场景切换时被销毁
            Canvas rootCanvas = loadingPanel.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                DontDestroyOnLoad(rootCanvas.gameObject);
            else
                DontDestroyOnLoad(loadingPanel.gameObject);
        }
        else
        {
            Debug.LogError("LoadingPanel 未赋值！");
        }
    }

    /// <summary>
    /// 加载场景（自动保存当前状态）
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string targetScene)
    {
        isLoading = true;
        //等这玩意儿初始化完成
        while (!LocalizationManager.Instance.IsInitialized)
            yield return null;

        // ---- 淡入动画 ----
        loadingPanel.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        float elapsedFade = 0f;
        while (elapsedFade < fadeInDuration)
        {
            elapsedFade += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsedFade / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(0.1f);

        // 淡入完成，开始加载流程
        float startTime = Time.time;

        // 保存当前游戏状态（根据需要启用）
        // SaveCurrentGameState();

        loadingPanel.SetProgress(0f);
        loadingPanel.SetTip("正在寻找记忆丝线...");

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / minimumLoadTime);                // 基于时间的虚拟进度
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);     // 真实加载进度
            float displayProgress = Mathf.Min(t, realProgress);                // 显示进度（取两者较小值）
            loadingPanel.SetProgress(displayProgress);

            string tipKey = "";

            // 根据显示进度更换提示文本（可选）
            if (displayProgress < 0.3f)
                tipKey = "0";
            //loadingPanel.SetTip("梳理记忆脉络...");
            else if (displayProgress < 0.6f)
                tipKey = "1";
            //loadingPanel.SetTip("编织羁绊之网...");
            else if (displayProgress < 0.9f)
                tipKey = "2";
            //loadingPanel.SetTip("点亮星辰坐标...");
            else
                tipKey = "3";
            //loadingPanel.SetTip("即将抵达...");

            // 从管理器获取当前语言的文本（同步，因为已初始化）
            string tipText = LocalizationManager.Instance.GetText("LoadingPanel", tipKey);
            loadingPanel.SetTip(tipText);

            if (operation.progress >= 0.90f && elapsed >= minimumLoadTime)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 场景激活后，恢复游戏状态（例如将玩家位置归零）
        RestoreGameStateForScene();

        yield return new WaitForSecondsRealtime(0.5f);

        // ---- 淡出动画 ----
        // 先禁用所有子物体（即内部 UI 元素），背景保留
        foreach (Transform child in loadingPanel.transform)
        {
            child.gameObject.SetActive(false);
        }

        elapsedFade = 0f;
        while (elapsedFade < fadeOutDuration)
        {
            elapsedFade += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsedFade / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // 隐藏整个面板
        loadingPanel.gameObject.SetActive(false);
        isLoading = false;
    }

    /// <summary>
    /// 保存当前游戏状态到 SaveManager
    /// </summary>
    private void SaveCurrentGameState()
    {
        // 其他数据（如收集物）也可在此保存
        // SaveManager.Instance.SaveGame(data);
    }

    /// <summary>
    /// 新场景加载后，恢复玩家位置等状态
    /// </summary>
    private void RestoreGameStateForScene()
    {
        // 查找名为 "Player" 的游戏对象
        GameObject player = GameObject.Find("Player");

        if (player != null)
        {
            Vector3 position = player.transform.position;
            position.x = 0f;
            position.y = 0f;
            player.transform.position = position;
            Debug.Log("已将 Player 的位置重置为 (0, 0, " + position.z + ")");
        }
        else
        {
            Debug.LogWarning("未找到名为 'Player' 的对象！");
        }
    }
}