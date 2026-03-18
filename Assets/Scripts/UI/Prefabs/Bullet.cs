using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public string bulletId;       // 子弹ID（从定义读取）
    [HideInInspector] public float speed;           // 速度
    [HideInInspector] public int damage;            // 伤害值
    [HideInInspector] public GameObject owner;      // 发射者（用于判断阵营）

    public List<SpellModuleSO> modules;   // 要应用的模块列表（在生成时传入）
    private float lifeTimer;
    private Transform target;              // 用于追踪的目标（最近敌人）

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 设置生命周期自动销毁
        Destroy(gameObject, 2f); // 临时值，实际可从定义读取


        // 如果有模块，可能需要立即应用一些效果，比如分裂
        foreach (var module in modules)
        {
            switch (module.moduleType)
            {
                case SpellModuleType.Split:
                    // 分裂：生成额外子弹
                    SpawnSplitBullets();
                    break;
                case SpellModuleType.Burst:
                    // 爆裂：生成多发子弹（可延迟生成）
                    StartCoroutine(BurstCoroutine());
                    break;
                    // 其他模块在 Update 中处理
            }
        }
    }

    void Update()
    {
        // 每帧应用模块效果
        foreach (var module in modules)
        {
            ApplyModuleEffect(module);
        }
    }

    /// <summary>
    /// 初始化子弹（由生成者调用）
    /// </summary>
    public void Initialize(Vector2 direction, GameObject owner, float speed, int damage, List<SpellModuleSO> modules = null)
    {
        this.owner = owner;
        this.speed = speed;
        this.damage = damage;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = direction.normalized * speed;
        this.modules = modules ?? new List<SpellModuleSO>();

        // 可选：根据模块初始化一些属性
        ApplyInitialModules();
    }



    private void ApplyModuleEffect(SpellModuleSO module)
    {
        switch (module.moduleType)
        {
            case SpellModuleType.Homing:
                ApplyHoming(module.homingStrength);
                break;
            case SpellModuleType.Rotate:
                ApplyRotation(module.rotateSpeed);
                break;
            case SpellModuleType.SpeedUp:
                // 加速可以在生成时一次性应用，但也可以持续
                break;
                // 其他
        }
    }


    private void ApplyInitialModules()
    {
        // 有些模块可能需要在生成时立即生效，如分裂（立即生成额外子弹）
        // 为了安全，我们在 Start 中处理
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 根据发射者判断伤害对象
        if (owner.CompareTag("Player"))
        {
            // 玩家发射的子弹伤害敌人
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                CombatManager.Instance.ApplyDamage(owner, enemy.gameObject, damage);
                Destroy(gameObject);
            }
            // 可扩展：击中墙壁等也销毁
        }
        else if (owner.CompareTag("Enemy"))
        {
            // 敌人发射的子弹伤害玩家
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                CombatManager.Instance.ApplyDamage(owner, player.gameObject, damage);
                Destroy(gameObject);
            }
        }
    }


    private void ApplyHoming(float strength)
    {
        if (target == null)
        {
            // 查找最近的敌人（可根据需求实现）
            FindNearestEnemy();
        }
        if (target != null)
        {
            Vector2 dirToTarget = (target.position - transform.position).normalized;
            rb.velocity = Vector2.Lerp(rb.velocity, dirToTarget * speed, strength * Time.deltaTime);
        }
    }

    private void FindNearestEnemy()
    {
        // 简单查找：使用 OverlapCircleAll 查找 Enemy
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1000f);
        float minDist = float.MaxValue;
        Enemy nearest = null;
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }
        }
        if (nearest != null) target = nearest.transform;
    }

    private void ApplyRotation(float speed)
    {
        // 旋转子弹速度方向
        float angle = speed * Time.deltaTime;
        rb.velocity = Quaternion.Euler(0, 0, angle) * rb.velocity;
    }

    private void SpawnSplitBullets()
    {
        // 示例：分裂出两个子弹，与原方向左右偏移 splitAngle 度
        // 注意：需要获取原模块参数，这里简单使用预设值，实际应从模块读取
        // 由于没有模块参数，我们需要模块数据。可在模块列表中查找 Split 模块。
        SpellModuleSO splitModule = modules.Find(m => m.moduleType == SpellModuleType.Split);
        if (splitModule != null)
        {
            float angle = splitModule.splitAngle;
            Vector2 dirLeft = Quaternion.Euler(0, 0, angle) * rb.velocity.normalized;
            Vector2 dirRight = Quaternion.Euler(0, 0, -angle) * rb.velocity.normalized;
            // 生成两颗新子弹，不带分裂模块（避免无限分裂），但可以带其他模块
            SpawnChildBullet(dirLeft);
            SpawnChildBullet(dirRight);
        }
        // 原子弹是否保留？按需，可以销毁原子弹或保留。这里保留原子弹。
    }

    private void SpawnChildBullet(Vector2 dir)
    {
        // 复制当前子弹，但移除分裂模块以避免循环分裂
        GameObject child = Instantiate(gameObject, transform.position, Quaternion.identity);
        Bullet childBullet = child.GetComponent<Bullet>();
        // 移除分裂模块（递归分裂？根据需要）
        List<SpellModuleSO> childModules = new List<SpellModuleSO>(modules);
        childModules.RemoveAll(m => m.moduleType == SpellModuleType.Split); // 避免无限分裂
        childModules.RemoveAll(m => m.moduleType == SpellModuleType.Burst); // 避免无限分裂
        childBullet.modules = childModules;
        childBullet.Initialize(dir, owner, speed, damage, childModules);
        // 设置生命周期等
    }

    private IEnumerator BurstCoroutine()
    {
        // 爆裂：连续生成多发子弹
        SpellModuleSO burstModule = modules.Find(m => m.moduleType == SpellModuleType.Burst);
        if (burstModule != null)
        {
            int t = burstModule.burstCount;
            for (int i = 0; i < t; i++)
            {
                // 生成新子弹，方向随机偏移？可根据需求实现
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                SpawnChildBullet(randomDir);
                yield return new WaitForSeconds(burstModule.burstDelay);
            }
        }
        // 爆裂后原子弹可销毁？这里保留原子弹。
    }
}