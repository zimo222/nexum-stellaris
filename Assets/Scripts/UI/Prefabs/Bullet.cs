using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public string bulletId;       // 子弹ID（从定义读取）
    [HideInInspector] public float speed;           // 速度
    [HideInInspector] public int damage;            // 伤害值
    [HideInInspector] public GameObject owner;      // 发射者（用于判断阵营）


    // 轨道运动专用字段
    private Vector2 firePoint;           // 发射点（圆心）
    private float orbitAngle;             // 当前角度（度）
    private bool isOrbiting = false;      // 是否正在圆周运动
    private float orbitRadius;            // 轨道半径
    private float actualOrbitRadius;   // 实际轨道半径（随机后）
    private float actualRotateSpeed;   // 实际旋转速度（随机后）

    public List<SpellModuleSO> modules;   // 要应用的模块列表（在生成时传入）
    private float lifeTimer;
    private Transform target;              // 用于追踪的目标（最近敌人）

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 设置生命周期自动销毁
        Destroy(gameObject, 5f); // 临时值，实际可从定义读取


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
    public void Initialize(Vector2 direction, GameObject owner, float speed, int damage, Vector2 spawnPos, List<SpellModuleSO> modules = null)
    {
        this.owner = owner;
        this.speed = speed * Random.Range(0.8f, 1.2f);
        this.damage = damage;
        this.firePoint = spawnPos;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = direction.normalized * speed * Random.Range(0.8f, 1.2f);
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
                ApplyRotation(module); // 传入模块对象
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
    /*
    private void ApplyRotation(float speed)
    {
        // 旋转子弹速度方向
        float angle = speed * Time.deltaTime;
        rb.velocity = Quaternion.Euler(0, 0, angle) * rb.velocity;
    }
    */

    private void ApplyRotation(SpellModuleSO module)
    {
        // 检查是否启用圆周运动（orbitRadius > 0）
        if (module.orbitRadius > 0)
        {
            // 第一次进入圆周模式时初始化
            if (!isOrbiting)
            {
                isOrbiting = true;
                firePoint = transform.position;      // 记录发射点（圆心）

                // 生成随机因子（0.8 ~ 1.2）
                float radiusRand = Random.Range(0.9f, 1.1f);
                float speedRand = Random.Range(0.9f, 1.1f);
                actualOrbitRadius = module.orbitRadius * radiusRand;
                actualRotateSpeed = module.rotateSpeed * speedRand;

                // 根据当前速度方向确定初始角度
                Vector2 dir = rb.velocity.normalized;
                orbitAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // 立即将子弹放到圆周上（发射点 + 随机半径 * 方向）
                Vector2 offset = Quaternion.Euler(0, 0, orbitAngle) * Vector2.right * actualOrbitRadius;
                transform.position = firePoint + offset;

                // 切换为 Kinematic，避免物理干扰
                rb.isKinematic = true;
                rb.velocity = Vector2.zero;

                // 使子弹面向切线方向（运动方向）
                float rad = orbitAngle * Mathf.Deg2Rad;
                Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
                transform.up = tangent; // 若精灵方向不同，可改为 transform.right
            }

            // 每帧更新角度，计算新位置
            orbitAngle += actualRotateSpeed * Time.deltaTime;
            Vector2 newOffset = Quaternion.Euler(0, 0, orbitAngle) * Vector2.right * actualOrbitRadius;
            transform.position = firePoint + newOffset;

            // 更新面向方向
            float newRad = orbitAngle * Mathf.Deg2Rad;
            Vector2 newTangent = new Vector2(-Mathf.Sin(newRad), Mathf.Cos(newRad));
            transform.up = newTangent;

            return; // 圆周模式下不再执行旧旋转逻辑
        }

        // 旧逻辑：普通速度旋转（未启用圆周时）
        float angle = module.rotateSpeed * Time.deltaTime;
        rb.velocity = Quaternion.Euler(0, 0, angle) * rb.velocity;
    }

    private void SpawnSplitBullets()
    {
        SpellModuleSO splitModule = modules.Find(m => m.moduleType == SpellModuleType.Split);
        if (splitModule != null)
        {
            float totalAngle = splitModule.splitAngle; // 总角度范围（从 -angle 到 angle）
            int count = splitModule.splitCount;
            if (count <= 0) return;

            // 基准方向（当前子弹速度方向）
            Vector2 baseDir = rb.velocity.normalized;

            // 如果只有一个子弹，可以直接在中间
            if (count == 1)
            {
                SpawnChildBullet(baseDir);
            }
            else
            {
                // 计算每个子弹的角度偏移
                // 角度间隔 = totalAngle * 2 / (count - 1) ？不对，因为区间是从 -totalAngle 到 +totalAngle，总跨度是 2*totalAngle。
                // 均匀分布意味着第一个子弹在 -totalAngle，最后一个在 +totalAngle，中间等间距。
                // 间隔 delta = (2 * totalAngle) / (count - 1)
                // 注意：如果 count=1，不应该进入此分支，已在上面处理。
                float delta = (2 * totalAngle) / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    // 当前子弹的角度偏移：从 -totalAngle 开始，每次增加 delta
                    float angleOffset = -totalAngle + i * delta;
                    Vector2 dir = Quaternion.Euler(0, 0, angleOffset) * baseDir;
                    SpawnChildBullet(dir);
                }
            }
        }
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
        childBullet.Initialize(dir, owner, speed, damage, firePoint, childModules);
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