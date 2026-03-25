using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    // 单例
    public static BulletPool Instance { get; private set; }

    [Header("池配置")]
    [SerializeField] private GameObject bulletPrefab;   // 要池化的子弹预制体
    [SerializeField] private int initialPoolSize = 20;  // 初始创建数量

    private Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        // 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 切换场景不销毁，保持池
    }

    private void Start()
    {
        // 预创建指定数量的子弹
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject obj = CreateNewBullet();
            pool.Enqueue(obj);
        }
    }

    /// <summary>
    /// 创建一颗新子弹（初始为未激活状态）
    /// </summary>
    private GameObject CreateNewBullet()
    {
        GameObject obj = Instantiate(bulletPrefab);
        obj.SetActive(false);           // 先隐藏
        return obj;
    }

    /// <summary>
    /// 从池中获取一颗子弹（如果池空则动态创建）
    /// </summary>
    public GameObject GetBullet()
    {
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            Debug.LogWarning("子弹池已空，动态创建新子弹");
            obj = CreateNewBullet();
        }
        obj.SetActive(true);   // 激活
        return obj;
    }

    /// <summary>
    /// 将子弹放回池中（自动隐藏并重置状态）
    /// </summary>
    public void ReturnBullet(GameObject obj)
    {
        // 调用子弹自己的重置方法
        Bullet bullet = obj.GetComponent<Bullet>();
        if (bullet != null)
            bullet.ResetToPool();

        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}