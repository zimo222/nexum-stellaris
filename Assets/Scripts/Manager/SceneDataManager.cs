using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneDataManager : Singleton<SceneDataManager>
{
    [Header("Loading Settings")]
    [SerializeField] private LoadingPanel loadingPanel;  // 拖入加载面板
    [SerializeField] private float minimumLoadTime = 1f; // 最短加载时间（避免一闪而过）

    private bool isLoading = false;

    protected override void Awake()
    {
        base.Awake();
        if (loadingPanel != null) loadingPanel.gameObject.SetActive(false);
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

        // 1. 保存当前游戏状态
        SaveCurrentGameState();

        // 2. 显示加载面板
        loadingPanel.gameObject.SetActive(true);
        loadingPanel.SetProgress(0f);
        loadingPanel.SetTip("正在寻找记忆丝线...");

        // 3. 开始异步加载
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);
        operation.allowSceneActivation = false; // 先不激活，等进度100%后再手动激活

        float elapsedTime = 0f;

        // 4. 更新进度条（progress 在 0~0.9 之间）
        while (!operation.isDone)
        {
            elapsedTime += Time.deltaTime;

            // 计算显示进度（0~0.9 映射到 0~1，剩余 0.1 是激活场景的瞬间）
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            loadingPanel.SetProgress(progress);

            // 根据进度更换提示文本（可选）
            if (progress < 0.3f)
                loadingPanel.SetTip("梳理记忆脉络...");
            else if (progress < 0.6f)
                loadingPanel.SetTip("编织羁绊之网...");
            else if (progress < 0.9f)
                loadingPanel.SetTip("点亮星辰坐标...");
            else
                loadingPanel.SetTip("即将抵达...");

            // 当进度达到 0.9（即加载完成）且满足最短加载时间，允许激活
            if (operation.progress >= 0.9f && elapsedTime >= minimumLoadTime)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        // 5. 场景激活后，恢复游戏状态（玩家位置等）
        RestoreGameStateForScene(targetScene);

        // 6. 隐藏加载面板
        loadingPanel.gameObject.SetActive(false);
        isLoading = false;
    }

    /// <summary>
    /// 保存当前游戏状态到 SaveManager
    /// </summary>
    private void SaveCurrentGameState()
    {
        GameData data = new GameData();
        data.currentScene = SceneManager.GetActiveScene().name;

        // 查找玩家位置（假设场景中有 Player 标签的对象）
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.playerPosX = player.transform.position.x;
            data.playerPosY = player.transform.position.y;
        }

        // 其他数据（如收集物）也可在此保存
        SaveManager.Instance.SaveGame(data);
    }

    /// <summary>
    /// 新场景加载后，恢复玩家位置等状态
    /// </summary>
    private void RestoreGameStateForScene(string sceneName)
    {
        GameData data = SaveManager.Instance.LoadGame(); // 加载最新的存档

        // 如果存档中的场景与当前场景一致，则设置玩家位置
        if (data.currentScene == sceneName)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(data.playerPosX, data.playerPosY, 0);
            }
        }
        else
        {
            // 场景不一致，可能使用默认出生点（由场景中的 SpawnPoint 决定）
            // 这里可以触发一个事件，让场景自己去处理
            Debug.LogWarning("存档场景与当前场景不符，使用默认出生点");
        }

        // 其他数据恢复（如收集物状态）可在此处广播事件，让各系统自行处理
    }
}