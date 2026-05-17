using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 为 ScriptableObject 提供统一的 Id 访问接口
/// 需要你的所有数据定义类（ExotextDefineSO等）实现此接口
/// </summary>
public interface IHaveId
{
    string Id { get; }
}

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    // 对外暴露的字典（与原接口完全一致）
    public Dictionary<string, ExotextDefineSO> ExotextDict { get; private set; }
    public Dictionary<string, NexusVestureDefineSO> NexusVestureDict { get; private set; }
    public Dictionary<string, MaterialDefineSO> MaterialDict { get; private set; }
    public Dictionary<string, QuestDefineSO> QuestDict { get; private set; }
    public Dictionary<string, BulletDefineSO> BulletDict { get; private set; }
    public Dictionary<string, SpellModuleSO> SpellModuleDict { get; private set; }
    public Dictionary<string, TutorialDefineSO> TutorialDict { get; private set; }
    public Dictionary<string, SpecialEffectDefineSO> SpecialEffectDict { get; private set; }

    // 标记数据是否加载完成
    public bool IsReady { get; private set; } = false;

    // 存储加载句柄，用于释放资源
    private List<AsyncOperationHandle> _loadHandles = new List<AsyncOperationHandle>();

    void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化为空字典，避免外部访问时报 NullReferenceException
        ExotextDict = new Dictionary<string, ExotextDefineSO>();
        NexusVestureDict = new Dictionary<string, NexusVestureDefineSO>();
        MaterialDict = new Dictionary<string, MaterialDefineSO>();
        QuestDict = new Dictionary<string, QuestDefineSO>();
        BulletDict = new Dictionary<string, BulletDefineSO>();
        SpellModuleDict = new Dictionary<string, SpellModuleSO>();
        TutorialDict = new Dictionary<string, TutorialDefineSO>();
        SpecialEffectDict = new Dictionary<string, SpecialEffectDefineSO>();

        // 开始异步加载所有数据（不阻塞 Awake）
        StartCoroutine(LoadAllDataCoroutine());
    }

    /// <summary>
    /// 协程加载所有数据字典
    /// </summary>
    private System.Collections.IEnumerator LoadAllDataCoroutine()
    {
        // 并行加载所有任务
        var exotextTask = LoadDictAsync<ExotextDefineSO>("ExotextDefineSO");
        var nexusTask = LoadDictAsync<NexusVestureDefineSO>("NexusVestureDefineSO");
        //var materialTask = LoadDictAsync<MaterialDefineSO>("MaterialDefineSO");
        var questTask = LoadDictAsync<QuestDefineSO>("QuestDefineSO");
        var bulletTask = LoadDictAsync<BulletDefineSO>("BulletDefineSO");
        var spellTask = LoadDictAsync<SpellModuleSO>("SpellModuleDefineSO");
        var tutorialTask = LoadDictAsync<TutorialDefineSO>("TutorialDefineSO");
        var effectTask = LoadDictAsync<SpecialEffectDefineSO>("SpecialEffectDefineSO");

        // 等待所有任务完成
        yield return new WaitUntil(() =>
            exotextTask.IsCompleted &&
            nexusTask.IsCompleted &&
            //materialTask.IsCompleted &&
            questTask.IsCompleted &&
            bulletTask.IsCompleted &&
            spellTask.IsCompleted &&
            tutorialTask.IsCompleted &&
            effectTask.IsCompleted
        );

        // 将结果赋值给公共字典（注意：如果任务出错，Result 可能为 null，我们保留空字典）
        if (exotextTask.Result != null) ExotextDict = exotextTask.Result;
        if (nexusTask.Result != null) NexusVestureDict = nexusTask.Result;
        //if (materialTask.Result != null) MaterialDict = materialTask.Result;
        if (questTask.Result != null) QuestDict = questTask.Result;
        if (bulletTask.Result != null) BulletDict = bulletTask.Result;
        if (spellTask.Result != null) SpellModuleDict = spellTask.Result;
        if (tutorialTask.Result != null) TutorialDict = tutorialTask.Result;
        if (effectTask.Result != null) SpecialEffectDict = effectTask.Result;

        IsReady = true;
        Debug.Log($"[{GetType().Name}] All game data loaded successfully from Addressables.");
    }

    /// <summary>
    /// 异步加载指定标签的所有资源，并转换为 Dictionary<string, T>
    /// </summary>
    private async Task<Dictionary<string, T>> LoadDictAsync<T>(string label) where T : UnityEngine.Object, IHaveId
    {
        try
        {
            // 加载所有带有此标签的 T 类型资源
            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            _loadHandles.Add(handle); // 存储句柄，便于后续释放
            var assets = await handle.Task;

            if (assets == null || assets.Count == 0)
            {
                Debug.LogWarning($"No assets found for label '{label}'. Make sure your ScriptableObjects are marked with Addressable and label '{label}'.");
                return new Dictionary<string, T>();
            }

            // 根据 Id 构建字典
            return assets.ToDictionary(asset => asset.Id);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to load Addressables with label '{label}': {ex.Message}");
            return new Dictionary<string, T>();
        }
    }

    /// <summary>
    /// 外部可调用此方法等待数据加载完成（例如在场景切换前）
    /// </summary>
    public async Task WaitUntilReady()
    {
        while (!IsReady)
            await Task.Yield();
    }

    private void OnDestroy()
    {
        // 释放所有 Addressables 加载句柄，避免内存泄漏
        foreach (var handle in _loadHandles)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        _loadHandles.Clear();
    }
}