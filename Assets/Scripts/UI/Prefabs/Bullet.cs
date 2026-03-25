using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public string bulletId;
    [HideInInspector] public float speed;
    [HideInInspector] public int damage;
    [HideInInspector] public GameObject owner;

    private Rigidbody2D rb;
    private List<SpellModuleSO> correctors;          // 修正类列表
    private List<float> correctorTimers;             // 每个修正类的剩余延迟时间（从生成开始计时）
    private bool isInitialized = false;

    // 轨道运动专用字段（由旋转修正使用）
    private Vector2 orbitCenter;
    private float orbitAngle;
    private float orbitRadius;
    private float orbitSpeed;
    private bool isOrbiting = false;

    private Coroutine lifeCoroutine;

    private IEnumerator AutoReturnToPool(float delay)
    {
        yield return new WaitForSeconds(delay);
        BulletPool.Instance.ReturnBullet(gameObject);
    }

    void Start()
    {
        if (!isInitialized)
        {
            Debug.LogError("Bullet未正确初始化，请使用Initialize方法");
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // 更新修正类计时器
        for (int i = correctorTimers.Count - 1; i >= 0; i--)
        {
            correctorTimers[i] -= Time.deltaTime;
            if (correctorTimers[i] <= 0)
            {
                // 执行对应的修正类
                ExecuteCorrector(correctors[i]);
                correctors.RemoveAt(i);
                correctorTimers.RemoveAt(i);
            }
        }

        // 如果是圆周运动，更新位置（由 ExecuteCorrector 启动）
        if (isOrbiting)
        {
            UpdateOrbit();
        }

        if (rb != null && rb.velocity != Vector2.zero)
        {
            // 计算速度方向的角度（单位：度）
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;

            // 获取当前欧拉角（局部空间，避免父物体干扰）
            Vector3 angles = transform.localEulerAngles;
            // 只修改 Z 分量
            angles.z = angle;
            // 重新赋值
            transform.localEulerAngles = angles;
        }
    }

    /// <summary>
    /// 初始化子弹（必须调用）
    /// </summary>
    public void Initialize(Vector2 direction, GameObject owner, float speed, int damage, Vector2 spawnPos, List<SpellModuleSO> correctors)
    {
        // 彻底重置状态（避免残留）
        ResetToPool();

        this.owner = owner;
        this.speed = speed;
        this.damage = damage;
        this.correctors = new List<SpellModuleSO>(correctors);
        this.correctorTimers = new List<float>();

        // 初始化计时器：每个修正类有自己的延迟（从当前时间开始累计）
        float currentTime = 0f;
        foreach (var corr in correctors)
        {
            currentTime += corr.delay; // 按顺序累加延迟（即前一个执行后，再等 delay 执行下一个）
            correctorTimers.Add(currentTime);
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }

        isInitialized = true;

        // 设置生命周期自动销毁
        //Destroy(gameObject, 5f); // 可从 bulletDefine 读取，暂时写死

        // 取消之前的协程（如果有）
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);
        lifeCoroutine = StartCoroutine(AutoReturnToPool(5f));
        /*    
        // 设置初始朝向
        if (rb != null && rb.velocity != Vector2.zero)
        {
            transform.right = rb.velocity.normalized;
        }
        */
        if (rb != null && rb.velocity != Vector2.zero)
        {
            // 计算速度方向的角度（单位：度）
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;

            // 获取当前欧拉角（局部空间，避免父物体干扰）
            Vector3 angles = transform.localEulerAngles;
            // 只修改 Z 分量
            angles.z = angle;
            // 重新赋值
            transform.localEulerAngles = angles;
        }
    }

    private void ExecuteCorrector(SpellModuleSO corrector)
    {
        switch (corrector.moduleType)
        {
            case SpellModuleType.Corrector:
                // 根据具体参数执行效果
                if (corrector.splitCount > 0)
                {
                    Split(corrector);
                }
                if (corrector.homingStrength > 0)
                {
                    StartHoming(corrector);
                }
                if (corrector.rotateSpeed != 0)
                {
                    if (corrector.orbitRadius > 0)
                        StartOrbit(corrector);
                    else
                        StartRotate(corrector);
                }
                // 可扩展其他效果：加速、穿透等
                break;
        }
    }

    private void Split(SpellModuleSO corrector)
    {
        int count = corrector.splitCount;
        float totalAngle = corrector.splitAngle;
        Vector2 baseDir = rb.velocity.normalized;

        if (count <= 0) return;

        if (count == 1)
        {
            SpawnChildBullet(baseDir, corrector);
        }
        else
        {
            float delta = (2 * totalAngle) / (count - 1);
            for (int i = 0; i < count; i++)
            {
                float angleOffset = -totalAngle + i * delta;
                Vector2 dir = Quaternion.Euler(0, 0, angleOffset) * baseDir;
                SpawnChildBullet(dir, corrector);
            }
        }
    }

    private void SpawnChildBullet(Vector2 dir, SpellModuleSO corrector)
    {
        // 子子弹：基础属性相同，但修正类列表？通常分裂出的子弹是一个新投射，可以继承原子弹的修正类（未执行的）或为空。
        // 这里简化：子子弹不继承修正类，仅基础属性。
        GameObject child = Instantiate(gameObject, transform.position, Quaternion.identity);
        Bullet childBullet = child.GetComponent<Bullet>();
        childBullet.Initialize(dir, owner, speed, damage, transform.position, new List<SpellModuleSO>()); // 无修正类
        // 可选：让子子弹继承未执行的修正类？但可能造成无限递归，暂时不实现。
    }

    private void StartHoming(SpellModuleSO corrector)
    {
        // 启动追踪协程
        StartCoroutine(HomingCoroutine(corrector.homingStrength));
    }

    private IEnumerator HomingCoroutine(float strength)
    {
        while (true)
        {
            // 查找最近敌人
            Transform target = FindNearestEnemy();
            if (target != null)
            {
                Vector2 dirToTarget = (target.position - transform.position).normalized;
                rb.velocity = Vector2.Lerp(rb.velocity, dirToTarget * speed, strength * Time.deltaTime);
            }
            yield return null;
        }
    }

    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1000f);
        float minDist = float.MaxValue;
        Transform nearest = null;
        foreach (var hit in hits)
        {
            // 根据发射者判断敌人：如果发射者是玩家，敌人是Enemy；如果发射者是敌人，敌人是Player
            if (owner.CompareTag("Player") && hit.CompareTag("Enemy"))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hit.transform;
                }
            }
            else if (owner.CompareTag("Enemy") && hit.CompareTag("Player"))
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = hit.transform;
                }
            }
        }
        return nearest;
    }

    private void StartRotate(SpellModuleSO corrector)
    {
        StartCoroutine(RotateCoroutine(corrector.rotateSpeed));
    }

    private IEnumerator RotateCoroutine(float speed)
    {
        while (true)
        {
            float angle = speed * Time.deltaTime;
            rb.velocity = Quaternion.Euler(0, 0, angle) * rb.velocity;
            yield return null;
        }
    }

    private void StartOrbit(SpellModuleSO corrector)
    {
        isOrbiting = true;
        orbitCenter = transform.position; // 以当前位置为圆心
        orbitRadius = corrector.orbitRadius;
        orbitSpeed = corrector.orbitSpeed * Random.Range(0.5f, 2.0f);

        // 根据当前速度方向确定初始角度
        Vector2 dir = rb.velocity.normalized;
        orbitAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 立即将子弹放到圆周上
        Vector2 offset = Quaternion.Euler(0, 0, orbitAngle) * Vector2.right * orbitRadius;
        transform.position = orbitCenter + offset;

        // 切换为 Kinematic，避免物理干扰
        rb.isKinematic = true;
        rb.velocity = Vector2.zero;
    }

    private void UpdateOrbit()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;
        Vector2 newOffset = Quaternion.Euler(0, 0, orbitAngle) * Vector2.right * orbitRadius;
        transform.position = orbitCenter + newOffset;

        // 使子弹面向切线方向
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        transform.up = tangent;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;

        // 根据发射者判断伤害对象
        if (owner.CompareTag("Player") && other.CompareTag("Enemy"))
        {
            CombatManager.Instance.ApplyDamage(owner, other.gameObject, damage);
            BulletPool.Instance.ReturnBullet(gameObject);
        }
        else if (owner.CompareTag("Enemy") && other.CompareTag("Player"))
        {
            CombatManager.Instance.ApplyDamage(owner, other.gameObject, damage);
            BulletPool.Instance.ReturnBullet(gameObject);
        }
        // 可扩展：击中墙壁等也销毁
    }

    /// <summary>
    /// 重置子弹所有状态，准备放回池中
    /// </summary>
    public void ResetToPool()
    {
        // 停止所有协程（比如追踪、旋转等）
        StopAllCoroutines();

        // 重置物理状态
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false;   // 如果之前被设为运动学（如轨道运动），要恢复
        }

        // 重置标记
        isOrbiting = false;
        isInitialized = false;

        // 清空修正类相关列表
        if (correctors != null)
            correctors.Clear();
        if (correctorTimers != null)
            correctorTimers.Clear();

        // 可选：重置其他自定义字段（如轨道参数等）
        orbitCenter = Vector2.zero;
        orbitAngle = 0f;
        orbitRadius = 0f;
        orbitSpeed = 0f;
    }
}