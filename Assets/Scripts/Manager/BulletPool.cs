using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [Header("全局配置")]
    [SerializeField] private bool autoCreateOnEmpty = true;
    [SerializeField] private int defaultInitialSize = 10;

    [Header("预热配置 - 拖入子弹预制体并设置预热数量")]
    [SerializeField] private List<WarmupConfig> warmupConfigs = new List<WarmupConfig>();

    [System.Serializable]
    public class WarmupConfig
    {
        public GameObject prefab;
        public int warmupCount = 30;
    }

    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, int> totalCreated = new Dictionary<GameObject, int>();

    private GameObject poolContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        poolContainer = new GameObject("BulletPoolContainer");
        DontDestroyOnLoad(poolContainer);
        DontDestroyOnLoad(gameObject);

        foreach (var config in warmupConfigs)
        {
            if (config.prefab == null) continue;
            WarmupPool(config.prefab, config.warmupCount);
        }

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        CleanupUnusedBullets();
    }

    /// <summary>
    /// 清理所有池中未使用的子弹
    /// </summary>
    private void CleanupUnusedBullets()
    {
        // 关键修复：先获取所有键的副本，避免在遍历时修改字典
        List<GameObject> keys = pools.Keys.ToList();
        foreach (var prefab in keys)
        {
            if (!pools.ContainsKey(prefab)) continue;

            var queue = pools[prefab];
            var newQueue = new Queue<GameObject>();
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                if (obj != null)
                    Destroy(obj);
            }
            pools[prefab] = newQueue;
            totalCreated[prefab] = 0;
        }

        // 重新预热
        foreach (var config in warmupConfigs)
        {
            if (config.prefab == null) continue;
            WarmupPool(config.prefab, config.warmupCount);
        }
    }

    private void WarmupPool(GameObject prefab, int count)
    {
        if (!pools.ContainsKey(prefab))
        {
            var queue = new Queue<GameObject>();
            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNewBullet(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(poolContainer.transform);
                queue.Enqueue(obj);
            }
            pools[prefab] = queue;
            totalCreated[prefab] = count;
            Debug.Log($"预热子弹池: {prefab.name} 预创建 {count} 颗");
        }
        else
        {
            Queue<GameObject> queue = pools[prefab];
            int current = queue.Count;
            if (current < count)
            {
                for (int i = current; i < count; i++)
                {
                    GameObject obj = CreateNewBullet(prefab);
                    obj.SetActive(false);
                    obj.transform.SetParent(poolContainer.transform);
                    queue.Enqueue(obj);
                }
                totalCreated[prefab] = count;
            }
        }
    }

    public void RegisterBulletType(GameObject prefab, int initialSize = -1)
    {
        if (prefab == null) return;

        if (!pools.ContainsKey(prefab))
        {
            int size = initialSize >= 0 ? initialSize : defaultInitialSize;
            var queue = new Queue<GameObject>();
            for (int i = 0; i < size; i++)
            {
                GameObject obj = CreateNewBullet(prefab);
                obj.SetActive(false);
                obj.transform.SetParent(poolContainer.transform);
                queue.Enqueue(obj);
            }
            pools[prefab] = queue;
            totalCreated[prefab] = size;
            Debug.Log($"注册子弹类型 {prefab.name}，初始池大小 {size}");
        }
    }

    private GameObject CreateNewBullet(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.SetActive(false);
        return obj;
    }

    public GameObject GetBullet(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("GetBullet: prefab 为空");
            return null;
        }

        if (!pools.ContainsKey(prefab))
        {
            RegisterBulletType(prefab, defaultInitialSize);
        }

        Queue<GameObject> pool = pools[prefab];

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.transform.SetParent(null);
        }
        else
        {
            if (autoCreateOnEmpty)
            {
                obj = CreateNewBullet(prefab);
                totalCreated[prefab]++;
                Debug.LogWarning($"子弹类型 {prefab.name} 池已空，动态创建新实例（总数 {totalCreated[prefab]}）");
            }
            else
            {
                Debug.LogError($"子弹类型 {prefab.name} 池已空，且不允许自动创建");
                return null;
            }
        }

        obj.SetActive(true);
        return obj;
    }

    public void ReturnBullet(GameObject bulletObj, GameObject prefab)
    {
        if (bulletObj == null || prefab == null) return;

        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"ReturnBullet: 未找到预制体 {prefab.name} 的池，将直接销毁子弹");
            Destroy(bulletObj);
            return;
        }

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.ResetToPool();

        bulletObj.SetActive(false);
        bulletObj.transform.SetParent(poolContainer.transform);
        pools[prefab].Enqueue(bulletObj);
    }

    public void ClearAllPools()
    {
        foreach (var kvp in pools)
        {
            foreach (var obj in kvp.Value)
            {
                if (obj != null) Destroy(obj);
            }
        }
        pools.Clear();
        totalCreated.Clear();
    }
}