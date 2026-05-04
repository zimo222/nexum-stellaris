using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.Collections;

[DefaultExecutionOrder(-100)] // 让 Awake 尽早执行
public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 如果场景中没有，自动创建一个
                GameObject go = new GameObject("LocalizationManager");
                _instance = go.AddComponent<LocalizationManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private bool isInitialized = false;

    private void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 开始初始化 Localization 系统
        StartCoroutine(InitializeLocalization());
    }

    private IEnumerator InitializeLocalization()
    {
        // 等待 Localization 系统初始化完成
        yield return LocalizationSettings.InitializationOperation;
        isInitialized = true;
        Debug.Log($"LocalizationManager 初始化完成，当前语言: {LocalizationSettings.SelectedLocale.LocaleName}");
    }

    /// <summary>
    /// 同步获取本地化文本（需在初始化完成后调用）
    /// </summary>
    /// <param name="table">表名，例如 "LoadingPanel"</param>
    /// <param name="key">键名，例如 "tip_0"</param>
    /// <returns>当前语言对应的文本，如果失败则返回 key</returns>
    public string GetText(string table, string key)
    {
        if (!isInitialized)
        {
            Debug.LogError("LocalizationManager 尚未初始化完成，无法同步获取文本。请使用异步方法或等待初始化。");
            return key;
        }

        try
        {
            // 同步获取文本（表已加载时不会阻塞）
            return LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"获取本地化文本失败: table={table}, key={key}, 错误: {e.Message}");
            return key;
        }
    }

    /// <summary>
    /// 异步获取本地化文本（可在任意时刻调用，通过回调返回）
    /// </summary>
    /// <param name="table">表名</param>
    /// <param name="key">键名</param>
    /// <param name="onComplete">获取完成后的回调，参数为本地化文本</param>
    public void GetTextAsync(string table, string key, System.Action<string> onComplete)
    {
        StartCoroutine(GetTextAsyncCoroutine(table, key, onComplete));
    }

    private IEnumerator GetTextAsyncCoroutine(string table, string key, System.Action<string> onComplete)
    {
        // 等待初始化完成
        while (!isInitialized)
            yield return null;

        var asyncOp = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(table, key);
        yield return asyncOp;

        if (asyncOp.IsDone && asyncOp.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            onComplete?.Invoke(asyncOp.Result);
        }
        else
        {
            Debug.LogError($"异步获取文本失败: {asyncOp.OperationException}");
            onComplete?.Invoke(key);
        }
    }

    /// <summary>
    /// 当前管理器是否已初始化完成
    /// </summary>
    public bool IsInitialized => isInitialized;
}