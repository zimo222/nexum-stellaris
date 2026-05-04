using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private GameObject sourceEnemy;

    public void Initialize(Vector2 dir, float spd, int dmg, GameObject source)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        sourceEnemy = source;
        Destroy(gameObject, 5f); // 5秒后自动销毁
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                CombatManager.Instance.ApplyDamage(sourceEnemy, player.gameObject, damage);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}