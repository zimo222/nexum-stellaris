using UnityEngine;

public class EnemyDamageTrigger : MonoBehaviour
{
    public float damageInterval = 0.5f;   // 伤害间隔（秒）
    private float timer;
    private bool playerInRange = false;
    private Player player;
    private Enemy enemy;

    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        if (enemy == null)
            Debug.LogError("EnemyDamageTrigger must be a child of an Enemy!");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<Player>();
            playerInRange = true;
            timer = 0f; // 重置计时器
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
        }
    }

    void Update()
    {
        if (playerInRange && player != null)
        {
            timer += Time.deltaTime;
            if (timer >= damageInterval)
            {
                Debug.Log("你好啊小朋友");
                timer = 0f;
                // 造成伤害，伤害值取敌人的attackDamage
                CombatManager.Instance.ApplyDamage(enemy.gameObject, player.gameObject, enemy.attackDamage);
            }
        }
    }
}