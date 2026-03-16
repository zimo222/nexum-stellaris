using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public string bulletId;       // 子弹ID（从定义读取）
    [HideInInspector] public float speed;           // 速度
    [HideInInspector] public int damage;            // 伤害值
    [HideInInspector] public GameObject owner;      // 发射者（用于判断阵营）

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 设置生命周期自动销毁
        Destroy(gameObject, 2f); // 临时值，实际可从定义读取
    }

    /// <summary>
    /// 初始化子弹（由生成者调用）
    /// </summary>
    public void Initialize(Vector2 direction, GameObject owner, float speed, int damage)
    {
        this.owner = owner;
        this.speed = speed;
        this.damage = damage;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.velocity = direction.normalized * speed;
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
}