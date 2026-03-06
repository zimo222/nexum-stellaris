using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;

    [Header("Interaction")]
    public float interactRange = 2f;
    public LayerMask interactableLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 获取输入
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement.Normalize();

        // 交互检测（按下E键）
        if (Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            GameData data = new GameData();
            data.playerPosX = transform.position.x;
            data.playerPosY = transform.position.y;
            data.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SaveManager.Instance.SaveGame(data);
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            GameData data = SaveManager.Instance.LoadGame();
            transform.position = new Vector3(data.playerPosX, data.playerPosY, 0);
            // 还需要加载场景等其他数据，暂略
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    private void Interact()
    {
        // 在玩家周围检测可交互物体
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRange, interactableLayer);
        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.OnInteract();
                break; // 只交互第一个
            }
        }
    }

    // 可视化交互范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}