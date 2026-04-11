using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public string bulletId;
    [HideInInspector] public float speed;
    [HideInInspector] public int damage;
    [HideInInspector] public GameObject owner;
    [HideInInspector] public GameObject sourcePrefab;

    private Rigidbody2D rb;
    private bool isInitialized = false;
    private float startTime;
    private float totalLifeTime = 15f;

    private class CorrectorTiming
    {
        public SpellModuleSO module;
        public float startTime;
        public float endTime;
        public Coroutine activeCoroutine;
    }
    private List<CorrectorTiming> timings = new List<CorrectorTiming>();
    private CorrectorTiming currentActiveTiming = null;

    private Vector2 orbitCenter;
    private float orbitAngle;
    private float orbitRadius;
    private float orbitSpeed;
    private bool isOrbiting = false;

    private Coroutine lifeCoroutine;

    private List<SpellModuleSO> correctors;
    private List<float> correctorTimers;

    // ----------------------------------------------------------------------
    // 生命周期
    // ----------------------------------------------------------------------

    void Start()
    {
        if (!isInitialized)
        {
            Debug.LogError("Bullet未正确初始化，请使用Initialize方法");
            if (sourcePrefab != null)
                BulletPool.Instance.ReturnBullet(gameObject, sourcePrefab);
            else
                Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        UpdateActiveCorrector();

        if (isOrbiting)
        {
            UpdateOrbit();
        }

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

    public void Initialize(Vector2 direction, GameObject owner, float speed, int damage,
                          Vector2 spawnPos, List<SpellModuleSO> correctors, GameObject sourcePrefab,
                          float lifeTime = 5f)
    {
        ResetToPool();

        this.owner = owner;
        this.speed = speed;
        this.damage = damage;
        this.sourcePrefab = sourcePrefab;
        this.totalLifeTime = lifeTime;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        timings.Clear();
        float currentTime = 0f;
        for (int i = 0; i < correctors.Count; i++)
        {
            var corr = correctors[i];
            float start = currentTime;
            float end = (i == correctors.Count - 1) ? float.PositiveInfinity : currentTime + corr.delay;
            timings.Add(new CorrectorTiming { module = corr, startTime = start, endTime = end });
            currentTime += corr.delay;
        }

        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }

        isInitialized = true;
        startTime = Time.time;

        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);
        lifeCoroutine = StartCoroutine(AutoReturnToPool(totalLifeTime));

        if (rb != null && rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            Vector3 angles = transform.localEulerAngles;
            angles.z = angle;
            transform.localEulerAngles = angles;
        }
    }

    // ----------------------------------------------------------------------
    // 克隆方法（修复生命周期复制）
    // ----------------------------------------------------------------------
    
    public void CloneFrom(Bullet source)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("子弹预制体缺少 Rigidbody2D 组件！");
            BulletPool.Instance.ReturnBullet(gameObject, sourcePrefab);
            return;
        }

        ResetToPool();

        this.owner = source.owner;
        this.speed = source.speed;
        this.damage = source.damage;
        this.sourcePrefab = source.sourcePrefab;

        // ★ 关键修复：复制总寿命和起始时间，确保剩余时间一致
        this.totalLifeTime = source.totalLifeTime;
        this.startTime = source.startTime;

        this.timings.Clear();
        foreach (var timing in source.timings)
        {
            this.timings.Add(new CorrectorTiming
            {
                module = timing.module,
                startTime = timing.startTime,
                endTime = timing.endTime,
                activeCoroutine = null
            });
        }

        /*
        // ★ 过滤掉已经过期的一次性效果（避免子子弹重复触发）
        float elapsed = Time.time - startTime;
        this.timings.RemoveAll(t =>
        {
            // 一次性效果（endTime 为无穷大）且开始时间已过 → 移除
            if (float.IsPositiveInfinity(t.endTime) && t.startTime <= elapsed)
                return true;
            // 持续性效果如果已过结束时间（理论上不会，但安全起见也移除）
            if (!float.IsPositiveInfinity(t.endTime) && t.endTime <= elapsed)
                return true;
            return false;
        });

        */
        this.currentActiveTiming = null;
        if (source.currentActiveTiming != null)
        {
            int idx = source.timings.IndexOf(source.currentActiveTiming);
            if (idx >= 0 && idx < this.timings.Count)
                this.currentActiveTiming = this.timings[idx];
        }

        this.isOrbiting = source.isOrbiting;
        this.orbitCenter = source.orbitCenter;
        this.orbitAngle = source.orbitAngle;
        this.orbitRadius = source.orbitRadius;
        this.orbitSpeed = source.orbitSpeed;

        rb.velocity = source.rb.velocity;

        this.isInitialized = true;

        if (currentActiveTiming != null && IsContinuousEffect(currentActiveTiming.module))
        {
            currentActiveTiming.activeCoroutine = StartContinuousEffect(currentActiveTiming.module);
        }

        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);
        // 剩余生命周期 = totalLifeTime - 已存活时间
        float remaining = totalLifeTime - (Time.time - startTime);
        if (remaining < 0) remaining = 0;
        lifeCoroutine = StartCoroutine(AutoReturnToPool(remaining));
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

    // 放在类里的任意位置（或者单独的工具类）
    private Vector2 RotateTowards(Vector2 current, Vector2 target, float maxRadiansDelta)
    {
        float currentAngle = Mathf.Atan2(current.y, current.x);
        float targetAngle = Mathf.Atan2(target.y, target.x);
        float delta = Mathf.DeltaAngle(currentAngle * Mathf.Rad2Deg, targetAngle * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float newAngle = currentAngle + Mathf.Clamp(delta, -maxRadiansDelta, maxRadiansDelta);
        return new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle));
    }

    private IEnumerator HomingCoroutine(float strength)
    {
        while (true)
        {
            Transform target = FindNearestEnemy();
            if (target != null && rb != null && rb.velocity.magnitude > 0.01f)
            {
                float distance = Vector2.Distance(transform.position, target.position);

                // 超出追踪范围：不追踪
                if (distance >= 300f)
                {
                    yield return null;
                    continue;
                }

                // 距离系数：距离越近转向越快（0～1），距离 300 时为 0，距离 0 时为 1
                float distanceFactor = 1f - Mathf.Clamp01(distance / 300f);

                // 计算目标方向
                Vector2 dirToTarget = ((Vector2)target.position - (Vector2)transform.position).normalized;
                Vector2 currentDir = rb.velocity.normalized;

                // 关键参数：最大转角（弧度/秒）
                // strength 建议范围 0.5～3，值越大转向越快
                // 如果觉得转向太慢，可以乘以一个系数，比如 2f
                float maxTurnRadiansPerSec = strength * distanceFactor * 2f;   // 这里的 2f 是全局灵敏度，可调

                // 每帧最大转角
                float maxTurnThisFrame = maxTurnRadiansPerSec * Time.deltaTime;

                // 计算新方向
                Vector2 newDir = RotateTowards(currentDir, dirToTarget, maxTurnThisFrame);

                // 保持速度大小不变，只改变方向
                rb.velocity = newDir * rb.velocity.magnitude;
            }
            yield return null;
        }
    }

    private IEnumerator RotateCoroutine(float speed)
    {
        while (true)
        {
            if (rb == null) yield break;
            float angle = speed * Time.deltaTime;
            rb.velocity = Quaternion.Euler(0, 0, angle) * rb.velocity;
            yield return null;
        }
    }

    private IEnumerator OrbitCoroutine(SpellModuleSO module)
    {
        if (rb == null)
        {
            Debug.LogError("OrbitCoroutine: rb 为 null，无法进入轨道模式");
            yield break;
        }

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
        if (rb == null)
        {
            Debug.LogWarning("StopOrbit: rb 为 null，无法恢复运动");
            return;
        }

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
    // 分裂逻辑（添加日志便于调试）
    // ----------------------------------------------------------------------

    private void Split(SpellModuleSO corrector)
    {
        if (rb == null)
        {
            Debug.LogWarning("Split: rb 为 null，无法分裂");
            return;
        }

        int count = corrector.splitCount;
        float totalAngle = corrector.splitAngle;
        Vector2 baseDir = rb.velocity.normalized;

        if (count <= 0) return;

        Debug.Log($"分裂开始，目标数量: {count}");  // 调试用

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
        if (sourcePrefab == null)
        {
            Debug.LogError("SpawnChildBullet: sourcePrefab 为 null，无法生成子子弹");
            return;
        }

        GameObject childObj = BulletPool.Instance.GetBullet(sourcePrefab);
        childObj.transform.position = transform.position;
        childObj.transform.rotation = Quaternion.identity;

        Bullet childBullet = childObj.GetComponent<Bullet>();
        if (childBullet == null)
        {
            Debug.LogError("子子弹预制体没有 Bullet 组件");
            BulletPool.Instance.ReturnBullet(childObj, sourcePrefab);
            return;
        }

        childBullet.CloneFrom(this);

        if (childBullet.rb != null)
        {
            childBullet.rb.velocity = dir.normalized * speed;
        }

        if (childBullet.rb != null && childBullet.rb.velocity != Vector2.zero)
        {
            float angle = Mathf.Atan2(childBullet.rb.velocity.y, childBullet.rb.velocity.x) * Mathf.Rad2Deg;
            Vector3 angles = childBullet.transform.localEulerAngles;
            angles.z = angle;
            childBullet.transform.localEulerAngles = angles;
        }

        Debug.Log($"生成子子弹，方向: {dir}");  // 调试用
    }

    // ----------------------------------------------------------------------
    // 辅助属性
    // ----------------------------------------------------------------------
    public float ElapsedTime => Time.time - startTime;
    public float RemainingLife => totalLifeTime - ElapsedTime;

    // ----------------------------------------------------------------------
    // 辅助方法：寻找敌人
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
        if (sourcePrefab != null)
            BulletPool.Instance.ReturnBullet(gameObject, sourcePrefab);
        else
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized) return;

        if (owner.CompareTag("Player") && other.CompareTag("Enemy"))
        {
            CombatManager.Instance.ApplyDamage(owner, other.gameObject, damage);
            if (sourcePrefab != null)
                BulletPool.Instance.ReturnBullet(gameObject, sourcePrefab);
            else
                Destroy(gameObject);
        }
        else if (owner.CompareTag("Enemy") && other.CompareTag("Player"))
        {
            CombatManager.Instance.ApplyDamage(owner, other.gameObject, damage);
            if (sourcePrefab != null)
                BulletPool.Instance.ReturnBullet(gameObject, sourcePrefab);
            else
                Destroy(gameObject);
        }
    }

    public void ResetToPool()
    {
        if (currentActiveTiming != null && IsContinuousEffect(currentActiveTiming.module))
        {
            StopContinuousEffect(currentActiveTiming);
        }

        StopAllCoroutines();
        if (lifeCoroutine != null)
            StopCoroutine(lifeCoroutine);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = false;
        }

        isOrbiting = false;
        isInitialized = false;

        timings.Clear();
        currentActiveTiming = null;

        if (correctors != null)
            correctors.Clear();
        if (correctorTimers != null)
            correctorTimers.Clear();
    }
}