using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMover : MonoBehaviour
{
    private Rigidbody2D rb;
    private NPCAnimController animCtrl;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true;
        animCtrl = GetComponent<NPCAnimController>();
    }

    public IEnumerator MoveToPosition(Vector2 target, float speed)
    {
        float maxTime = 10f;          // 防止卡死
        float elapsed = 0f;

        // 如果已经非常接近目标，直接归位并结束
        if (Vector2.Distance(rb.position, target) < 0.05f)
        {
            rb.position = target;
            if (animCtrl != null) animCtrl.SetMovement(Vector2.zero);
            yield break;
        }

        while (elapsed < maxTime)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, target, speed * Time.deltaTime);
            rb.MovePosition(newPos);
            Vector2 dir = (target - rb.position).normalized;
            if (animCtrl != null) animCtrl.SetMovement(dir);

            if (Vector2.Distance(rb.position, target) < 0.05f)
            {
                rb.position = target;  // 强制归位
                break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保最终位置精确
        rb.position = target;
        if (animCtrl != null) animCtrl.SetMovement(Vector2.zero);
    }
}