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
    private bool isInitialized = false;
    private float startTime;                          // 子弹生成时的游戏时间

    // 修正类时序管理
    private class CorrectorTiming
    {
        public SpellModuleSO module;
        public float startTime;      // 相对子弹生成的时间（秒）
        public float endTime;        // 相对子弹生成的时间（秒），正无穷表示直到销毁
        public Coroutine activeCoroutine; // 持续效果对应的协程
    }
    private List<CorrectorTiming> timings = new List<CorrectorTiming>();
    private CorrectorTiming currentActiveTiming = null;

    // 轨道运动专用字段（由旋转修正使用）
    private Vector2 orbitCenter;
    private float orbitAngle;
    private float orbitRadius;
    private float orbitSpeed;
    private bool isOrbiting = false;

    private Coroutine lifeCoroutine;

    // ----------------------------------------------------------------------
    // 生命周期
    // ----------------------------------------------------------------------

    void Start()
    {
        if (!isInitialized)
        {
            Debug.LogError("Bullet未正确初始化，请使用Initialize方法");
            BulletPool.Instance.ReturnBullet(gameObject);
        }
    }

    void Update()
    {

        if (!isInitialized) return;

        // 更新当前激活的修正类（按时间段切换）
        UpdateActiveCorrector();

        //Debug.Log(currentActiveTiming.startTime.ToString() + "." + currentActiveTiming.endTime.ToString() + currentActiveTiming.module.id);
        // 如果是圆周运动，更新位置（由轨道协程或Update标志管理）
        if (isOrbiting)
        {
            UpdateOrbit();
        }

        // 更新子弹朝向
        if (rb != null && rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            Vector3 angles = transform.localEulerAngles;
            angles.z = angle;
            transform.localEulerAngles = angles;
        }
    }

    // ----------------------------------------------------------------------
    // 初始化
    // ----------------------------------------------------------------------

    public void Initialize(Vector2 direction, GameObject owner, float speed, int damage, Vector2 spawnPos, List<SpellModuleSO> correctors)
    {
        ResetToPool();  // 确保状态干净

        this.owner = owner;
        this.speed = speed;
        this.damage = damage;

        // 构建修正类时序列表
        timings.Clear();
        float currentTime = 0f;
        for (int i = 0; i < correctors.Count; i++)
        {
            var corr = correctors[i];
            float start = currentTime;
            float end = (i == correctors.Count - 1) ? float.PositiveInfinity : currentTime + corr.delay;
            timings.Add(new CorrectorTiming { module = corr, startTime = start, endTime = end });
            currentTime += corr.delay;

            //Debug.Log(start.ToString() + "." + end.ToString() + "." + corr.name);
        }


        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }

        isInitialized = true;
        startTime = Time.time;

        // 自动返回池的协程
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);
        lifeCoroutine = StartCoroutine(AutoReturnToPool(5f));

        // 设置初始朝向
        if (rb != null && rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            Vector3 angles = transform.localEulerAngles;
            angles.z = angle;
            transform.localEulerAngles = angles;
        }
    }

    // ----------------------------------------------------------------------
    // 修正类切换逻辑
    // ----------------------------------------------------------------------

    private void UpdateActiveCorrector()
    {
        float elapsed = Time.time - startTime;

        // 找到当前时间点应该激活的修正类
        CorrectorTiming newTiming = null;
        foreach (var timing in timings)
        {
            if (elapsed >= timing.startTime && elapsed < timing.endTime)
            {
                newTiming = timing;
                break;
            }
        }

        // 如果激活的没变，直接返回
        if (newTiming == currentActiveTiming)
            return;

        // 停用当前激活的修正类（如果有且为持续效果）
        if (currentActiveTiming != null && IsContinuousEffect(currentActiveTiming.module))
        {
            StopContinuousEffect(currentActiveTiming);
        }

        // 激活新的修正类
        currentActiveTiming = newTiming;
        if (currentActiveTiming != null)
        {
            if (IsContinuousEffect(currentActiveTiming.module))
            {
                currentActiveTiming.activeCoroutine = StartContinuousEffect(currentActiveTiming.module);
            }
            else
            {
                ExecuteOnceEffect(currentActiveTiming.module);
            }
        }
    }

    // 判断模块是否为持续效果
    private bool IsContinuousEffect(SpellModuleSO module)
    {
        // 根据参数判断：追踪、旋转、轨道都是持续的
        return module.homingStrength > 0 || module.rotateSpeed != 0 || module.orbitRadius > 0;
    }

    // 执行一次性效果
    private void ExecuteOnceEffect(SpellModuleSO module)
    {
        if (module.splitCount > 0)
        {
            Split(module);
        }
        // 可扩展其他一次性效果，如爆炸、穿透等
    }

    // 启动持续效果，返回协程引用
    private Coroutine StartContinuousEffect(SpellModuleSO module)
    {
        if (module.homingStrength > 0)
        {
            return StartCoroutine(HomingCoroutine(module.homingStrength));
        }
        else if (module.rotateSpeed != 0)
        {
            if (module.orbitRadius > 0)
                return StartCoroutine(OrbitCoroutine(module));
            else
                return StartCoroutine(RotateCoroutine(module.rotateSpeed));
        }
        return null;
    }

    // 停用持续效果（停止协程并清理状态）
    private void StopContinuousEffect(CorrectorTiming timing)
    {
        if (timing.activeCoroutine != null)
        {
            StopCoroutine(timing.activeCoroutine);
            timing.activeCoroutine = null;
        }

        // 如果是轨道运动，需要额外清理
        if (timing.module.orbitRadius > 0 && isOrbiting)
        {
            StopOrbit();
        }
    }

    // ----------------------------------------------------------------------
    // 持续效果协程（可被 StopCoroutine 中断）
    // ----------------------------------------------------------------------

    private IEnumerator HomingCoroutine(float strength)
    {
        while (true)
        {
            Transform target = FindNearestEnemy();
            if (target != null)
            {
                Vector2 dirToTarget = (target.position - transform.position).normalized;
                rb.velocity = Vector2.Lerp(rb.velocity, dirToTarget * speed, strength * Time.deltaTime);
            }
            yield return null;
        }
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

    private IEnumerator OrbitCoroutine(SpellModuleSO module)
    {
        // 进入轨道模式
        isOrbiting = true;
        orbitCenter = transform.position;
        orbitRadius = module.orbitRadius;
        orbitSpeed = module.orbitSpeed * Random.Range(0.5f, 2.0f);

        Vector2 dir = rb.velocity.normalized;
        orbitAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Vector2 offset = Quaternion.Euler(0, 0, orbitAngle) * Vector2.right * orbitRadius;
        transform.position = orbitCenter + offset;

        rb.isKinematic = true;
        rb.velocity = Vector2.zero;

        // 轨道持续运行，直到协程被停止
        while (true)
        {
            yield return null; // 实际更新在 UpdateOrbit 中，但协程需要保持运行
        }
    }

    // 轨道运动的实际位置更新（放在 Update 中）
    private void UpdateOrbit()
    {
        orbitAngle += orbitSpeed * Time.deltaTime;
        Vector2 newOffset = Quaternion.Euler(0, 0, orbitAngle) * Vector2.right * orbitRadius;
        transform.position = orbitCenter + newOffset;

        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        transform.up = tangent;
    }

    // 停止轨道运动，恢复物理飞行
    private void StopOrbit()
    {
        isOrbiting = false;

        // 计算当前切线速度（方向为圆周切线，大小等于原速度大小）
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector2 tangent = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        rb.isKinematic = false;
        rb.velocity = tangent * speed;

        // 恢复初始朝向（可选）
        if (rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            Vector3 angles = transform.localEulerAngles;
            angles.z = angle;
            transform.localEulerAngles = angles;
        }
    }

    // ----------------------------------------------------------------------
    // 一次性效果：分裂
    // ----------------------------------------------------------------------

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
        // 子子弹不继承修正类，仅基础属性
        GameObject child = BulletPool.Instance.GetBullet(); // 使用对象池
        child.transform.position = transform.position;
        child.transform.rotation = Quaternion.identity;

        Bullet childBullet = child.GetComponent<Bullet>();
        childBullet.Initialize(dir, owner, speed, damage, transform.position, new List<SpellModuleSO>());
    }

    // ----------------------------------------------------------------------
    // 辅助方法
    // ----------------------------------------------------------------------

    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1000f);
        float minDist = float.MaxValue;
        Transform nearest = null;
        foreach (var hit in hits)
        {
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

    // ----------------------------------------------------------------------
    // 对象池相关
    // ----------------------------------------------------------------------

    private IEnumerator AutoReturnToPool(float delay)
    {
        yield return new WaitForSeconds(delay);
        BulletPool.Instance.ReturnBullet(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;

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
    }

    public void ResetToPool()
    {
        // 停用当前激活的持续效果
        if (currentActiveTiming != null && IsContinuousEffect(currentActiveTiming.module))
        {
            StopContinuousEffect(currentActiveTiming);
        }

        // 停止所有协程
        StopAllCoroutines();
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);

        // 重置物理
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false;
        }

        // 重置标志
        isOrbiting = false;
        isInitialized = false;

        // 清空时序数据
        timings.Clear();
        currentActiveTiming = null;

        // 清空其他列表
        if (correctors != null)
            correctors.Clear();
        if (correctorTimers != null)
            correctorTimers.Clear(); // 注意原代码中可能没有定义 correcterTimers，需要补充
        // 这里原代码中 correctorTimers 变量名有误，应该是 correctorTimers
        // 我注意到你之前有 private List<float> correctorTimers; 但后来未使用，可以删除或保留。
        // 为了安全，我们先注释掉，如果你确实有 correctorTimers 变量，请取消注释并清空。
        // 建议在类中定义 private List<float> correctorTimers; 并在这里清空。
    }

    // 原代码中可能有 correctors 和 correctorTimers 的声明，这里补充（如果没有请忽略）
    private List<SpellModuleSO> correctors;     // 实际上已在上面使用，但声明可能在前面
    private List<float> correctorTimers;        // 原代码中可能已有，若没有请加上
}