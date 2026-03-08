using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动速度")]
    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    private void Awake()
    {
        // 获取 Rigidbody2D 组件（如果没有则添加一个）
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning("未找到 Rigidbody2D 组件，将自动添加一个。");
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        // 读取 WASD 输入（GetAxisRaw 返回 -1, 0 或 1，无平滑过渡）
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // 构建原始方向向量
        Vector2 input = new Vector2(moveX, moveY);

        // 归一化向量，使斜向移动速度与轴向一致
        // 如果输入为零，则保持零向量
        movement = input.normalized;
    }

    private void FixedUpdate()
    {
        // 在 FixedUpdate 中设置刚体速度，实现平滑移动
        rb.velocity = movement * speed;
    }
}