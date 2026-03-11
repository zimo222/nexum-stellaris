using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.IO;

public class SceneDataManager : Singleton<SceneDataManager>
{
    [Header("Loading Settings")]
    [SerializeField] private LoadingPanel loadingPanel;
    [SerializeField] private float minimumLoadTime = 3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private bool isLoading = false;
    private CanvasGroup canvasGroup;

    protected override void Awake()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null);
            Debug.LogWarning($"管理器对象 {gameObject.name} 不是根对象，已自动移动到根层级。");
        }

        base.Awake();

        if (gameObject.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(gameObject);
        }

        if (loadingPanel != null)
        {
            canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = loadingPanel.gameObject.AddComponent<CanvasGroup>();
            loadingPanel.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("LoadingPanel 未赋值！");
        }
    }

    public void LoadScene(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string targetScene)
    {
        string logPath = Application.persistentDataPath + "/load_log.txt";
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== Load Started at {System.DateTime.Now} ===");
        sb.AppendLine($"Target Scene (name): {targetScene}");

        // 检查场景是否在 Build Settings 中
        bool sceneExists = Application.CanStreamedLevelBeLoaded(targetScene);
        sb.AppendLine($"Scene exists in Build Settings: {sceneExists}");

        int buildIndex = -1;
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameOnly = Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameOnly == targetScene)
            {
                buildIndex = i;
                break;
            }
        }
        sb.AppendLine($"Scene build index: {buildIndex}");

        if (!sceneExists && buildIndex == -1)
        {
            sb.AppendLine($"错误：场景 \"{targetScene}\" 不在 Build Settings 中。加载已中止。");
            File.WriteAllText(logPath, sb.ToString());
            Debug.LogError($"场景 \"{targetScene}\" 不存在于 Build Settings 中，无法加载。");
            yield break;
        }

        isLoading = true;
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

        // 开始异步加载（允许自动激活）
        float startTime = Time.realtimeSinceStartup;
        sb.AppendLine($"Start Time (realtime): {startTime}");

        loadingPanel.SetProgress(0f);
        loadingPanel.SetTip("正在寻找记忆丝线...");

        AsyncOperation operation;
        if (buildIndex >= 0)
        {
            operation = SceneManager.LoadSceneAsync(buildIndex);
            sb.AppendLine($"Loading by build index: {buildIndex}");
        }
        else
        {
            operation = SceneManager.LoadSceneAsync(targetScene);
            sb.AppendLine($"Loading by scene name: {targetScene}");
        }

        if (operation == null)
        {
            sb.AppendLine("错误：LoadSceneAsync 返回 null！");
            File.WriteAllText(logPath, sb.ToString());
            Debug.LogError($"无法加载场景：{targetScene}，操作返回 null。");
            yield break;
        }

        operation.allowSceneActivation = true; // 允许场景自动激活

        // 【关键修改】UI 进度完全基于时间，持续至少 minimumLoadTime 秒
        float targetEndTime = startTime + minimumLoadTime;
        while (Time.realtimeSinceStartup < targetEndTime)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float progress = Mathf.Clamp01(elapsed / minimumLoadTime);
            loadingPanel.SetProgress(progress);

            // 根据进度更换提示文本
            string tipKey = progress < 0.3f ? "0" :
                            progress < 0.6f ? "1" :
                            progress < 0.9f ? "2" : "3";
            string tipText = LocalizationManager.Instance.GetText("LoadingPanel", tipKey);
            loadingPanel.SetTip(tipText);

            sb.AppendLine($"Frame: {Time.frameCount}, Realtime: {Time.realtimeSinceStartup:F4}, Elapsed: {elapsed:F4}, Progress: {progress:F4}, RealProgress: {operation.progress:F4}");

            yield return null;
        }

        // 确保最终进度为 100%
        loadingPanel.SetProgress(1f);
        loadingPanel.SetTip(LocalizationManager.Instance.GetText("LoadingPanel", "3"));

        // 等待场景真正加载完成（如果还未完成）
        while (!operation.isDone)
        {
            sb.AppendLine($"Waiting for scene to complete... RealProgress: {operation.progress:F4}");
            yield return null;
        }

        sb.AppendLine($"Scene activated at realtime: {Time.realtimeSinceStartup}");

        // 验证加载后的场景
        string loadedSceneName = SceneManager.GetActiveScene().name;
        sb.AppendLine($"Loaded scene name: {loadedSceneName}");
        if (loadedSceneName != targetScene)
        {
            sb.AppendLine($"错误：当前场景为 {loadedSceneName}，目标为 {targetScene}，加载可能失败！");
            Debug.LogError($"场景加载失败：当前仍处于 {loadedSceneName}，未能切换到 {targetScene}");
        }
        else
        {
            RestoreGameStateForScene(); // 恢复玩家位置等
        }

        File.WriteAllText(logPath, sb.ToString());

        yield return new WaitForSecondsRealtime(0.5f); // 短暂停留

        // ---- 淡出动画 ----
        foreach (Transform child in loadingPanel.transform)
            child.gameObject.SetActive(false);

        elapsedFade = 0f;
        while (elapsedFade < fadeOutDuration)
        {
            elapsedFade += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsedFade / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        loadingPanel.gameObject.SetActive(false);
        isLoading = false;
    }

    // 保留原有占位方法
    private void SaveCurrentGameState() { }

    private void RestoreGameStateForScene()
    {
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