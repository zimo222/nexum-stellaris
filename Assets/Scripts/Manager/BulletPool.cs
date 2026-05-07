using System;
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [Header("预制体配置 - 直接拖拽引用")]
    [SerializeField] private List<BulletPrefabConfig> bulletPrefabs = new List<BulletPrefabConfig>();

    [Header("全局配置")]
    [SerializeField] private bool autoCreateOnEmpty = true;
    [SerializeField] private int defaultInitialSize = 30;

    [Serializable]
    public class BulletPrefabConfig
    {
        [Tooltip("子弹预制体")]
        public GameObject prefab;

        [Tooltip("预创建数量（热身）")]
        public int warmupCount = 50;

        [Tooltip("最大池容量（超过此数量不再归还，直接销毁）")]
        public int maxPoolSize = 200;

        // 运行时数据
        [NonSerialized] public Queue<GameObject> pool;
        [NonSerialized] public int totalCreated;
    }

    private Dictionary<GameObject, BulletPrefabConfig> configMap = new Dictionary<GameObject, BulletPrefabConfig>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePools();
    }

    /// <summary>
    /// 初始化所有池，预热到指定数量
    /// </summary>
    private void InitializePools()
    {
        foreach (var config in bulletPrefabs)
        {
            if (config.prefab == null)
            {
                Debug.LogWarning("子弹预制体配置为空，已跳过");
                continue;
            }

            config.pool = new Queue<GameObject>();

            // 预热：创建指定数量的子弹
            for (int i = 0; i < config.warmupCount; i++)
            {
                GameObject obj = CreateNewBullet(config.prefab);
                obj.SetActive(false);
                config.pool.Enqueue(obj);
            }

            config.totalCreated = config.warmupCount;
            configMap[config.prefab] = config;

            Debug.Log($"子弹池已预热: {config.prefab.name} | 预创建 {config.warmupCount} 颗 | 最大容量 {config.maxPoolSize}");
        }
    }

    /// <summary>
    /// 创建一颗新子弹（不激活）
    /// </summary>
    private GameObject CreateNewBullet(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        return obj;
    }

    /// <summary>
    /// 从池中获取一颗子弹
    /// </summary>
    public GameObject GetBullet(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("GetBullet: prefab 为空");
            return null;
        }

        // 获取配置
        if (!configMap.TryGetValue(prefab, out var config))
        {
            if (autoCreateOnEmpty)
            {
                // 动态创建临时配置（不推荐，建议在Inspector中配置）
                config = new BulletPrefabConfig
                {
                    prefab = prefab,
                    warmupCount = defaultInitialSize,
                    maxPoolSize = 200,
                    pool = new Queue<GameObject>(),
                    totalCreated = 0
                };
                configMap[prefab] = config;

                for (int i = 0; i < defaultInitialSize; i++)
                {
                    GameObject obj = CreateNewBullet(prefab);
                    obj.SetActive(false);
                    config.pool.Enqueue(obj);
                }
                config.totalCreated = defaultInitialSize;
                Debug.LogWarning($"子弹类型 {prefab.name} 未在Inspector中配置，已动态创建池，大小 {defaultInitialSize}");
            }
            else
            {
                Debug.LogError($"子弹类型 {prefab.name} 未注册且不允许自动创建");
                return null;
            }
        }

        // 从池中取出
        GameObject bulletObj;
        if (config.pool.Count > 0)
        {
            bulletObj = config.pool.Dequeue();
        }
        else
        {
            if (autoCreateOnEmpty)
            {
                bulletObj = CreateNewBullet(prefab);
                config.totalCreated++;
                Debug.LogWarning($"子弹类型 {prefab.name} 池已空，动态创建新实例（总数 {config.totalCreated}）");
            }
            else
            {
                Debug.LogError($"子弹类型 {prefab.name} 池已空，且不允许自动创建");
                return null;
            }
        }

        bulletObj.SetActive(true);
        return bulletObj;
    }

    /// <summary>
    /// 将子弹放回池中
    /// </summary>
    public void ReturnBullet(GameObject bulletObj, GameObject prefab)
    {
        if (bulletObj == null || prefab == null) return;

        if (!configMap.TryGetValue(prefab, out var config))
        {
            Debug.LogWarning($"ReturnBullet: 未找到预制体 {prefab.name} 的配置，直接销毁");
            Destroy(bulletObj);
            return;
        }

        // 检查池容量限制
        if (config.pool.Count >= config.maxPoolSize)
        {
            // 超过最大容量，直接销毁
            Destroy(bulletObj);
            return;
        }

        // 重置子弹状态
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.ResetToPool();

        bulletObj.SetActive(false);
        config.pool.Enqueue(bulletObj);
    }

    /// <summary>
    /// 获取池中某预制体的当前数量（调试用）
    /// </summary>
    public int GetPoolCount(GameObject prefab)
    {
        if (configMap.TryGetValue(prefab, out var config))
            return config.pool.Count;
        return 0;
    }

    /// <summary>
    /// 获取某预制体总共创建的数量（调试用）
    /// </summary>
    public int GetTotalCreated(GameObject prefab)
    {
        if (configMap.TryGetValue(prefab, out var config))
            return config.totalCreated;
        return 0;
    }

    /// <summary>
    /// 清空所有池（场景切换时调用）
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var config in bulletPrefabs)
        {
            if (config.pool != null)
            {
                while (config.pool.Count > 0)
                {
                    var obj = config.pool.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
        }
        configMap.Clear();
        InitializePools(); // 重新初始化
    }
}