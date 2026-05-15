using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NPCAnimController : MonoBehaviour
{
    private Animator animator;  // 引用于Image子物体的Animator
    private Vector2 lastMoveDir;
    private bool isMoving;

    void Awake()
    {
        // 查找子物体中的Animator（通常挂在Image上）
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError($"NPCAnimController: 在 {name} 的子物体中未找到 Animator");
    }

    public void SetMovement(Vector2 velocity)
    {
        isMoving = velocity.sqrMagnitude > 0.01f;
        if (isMoving)
            lastMoveDir = velocity.normalized;

        if (animator != null)
        {
            animator.SetBool("Idle", !isMoving);
            animator.SetBool("Move", isMoving);
            animator.SetFloat("xDir", lastMoveDir.x);
            animator.SetFloat("yDir", lastMoveDir.y);
        }
    }
}