using UnityEngine;

public class PlayerEscapeTrigger : MonoBehaviour
{
    private void Start()
    {
        // 确保碰撞器被设置为触发器
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("CapsuleCollider2D 的 isTrigger 属性未勾选，请将其设置为触发器。");
        }

        // 提醒添加 Rigidbody2D（物理系统需要至少一方有 Rigidbody2D 才能触发事件）
        if (GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogWarning("建议为当前对象添加 Rigidbody2D 组件（可设置为 Kinematic）以确保触发器正常工作。");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 检查离开的对象是否位于 "Player" 图层
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")  && other.gameObject.name == "Player")
        {
            // 调用战斗失败逻辑
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.CombatFailed();
            }
            else
            {
                Debug.LogError("CombatManager.Instance 为空，请确保 CombatManager 已正确初始化。");
            }
        }
    }
}