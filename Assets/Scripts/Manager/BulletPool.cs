using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [Header("全局配置")]
    [SerializeField] private bool autoCreateOnEmpty = true;   // 池空时自动创建新实例
    [SerializeField] private int defaultInitialSize = 10;     // 默认初始大小

    // 每个预制体对应一个池（队列）
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    // 可选：记录每个预制体创建的总数，用于调试
    private Dictionary<GameObject, int> totalCreated = new Dictionary<GameObject, int>();

    private void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 注册一种子弹类型，并预创建一定数量的实例
    /// </summary>
    /// <param name="prefab">子弹预制体</param>
    /// <param name="initialSize">初始池大小（可选，若不传则使用 defaultInitialSize）</param>
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
                queue.Enqueue(obj);
            }
            pools[prefab] = queue;
            totalCreated[prefab] = size;
            Debug.Log($"注册子弹类型 {prefab.name}，初始池大小 {size}");
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
    /// <param name="prefab">子弹预制体</param>
    /// <returns>可用的子弹对象</returns>
    public GameObject GetBullet(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("GetBullet: prefab 为空");
            return null;
        }

        // 确保池存在（如果从未注册，自动注册并创建默认数量）
        if (!pools.ContainsKey(prefab))
        {
            RegisterBulletType(prefab, defaultInitialSize);
        }

        Queue<GameObject> pool = pools[prefab];

        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
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

    /// <summary>
    /// 将子弹放回池中
    /// </summary>
    /// <param name="bulletObj">子弹对象</param>
    /// <param name="prefab">对应的预制体（用于定位池）</param>
    public void ReturnBullet(GameObject bulletObj, GameObject prefab)
    {
        if (bulletObj == null || prefab == null) return;

        // 确保池存在（理论上应该已经存在）
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"ReturnBullet: 未找到预制体 {prefab.name} 的池，将直接销毁子弹");
            Destroy(bulletObj);
            return;
        }

        // 重置子弹状态
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.ResetToPool();

        bulletObj.SetActive(false);
        pools[prefab].Enqueue(bulletObj);
    }

    /// <summary>
    /// 可选：清空所有池（场景切换时可能需要）
    /// </summary>
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