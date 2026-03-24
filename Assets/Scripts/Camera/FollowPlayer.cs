using UnityEngine;

public class FollowPlayer2D : MonoBehaviour
{
    public Transform player;          // 可手动拖拽赋值，如果不赋值则自动查找
    public Vector3 offset = new Vector3(0, 0, 0); // 相机相对玩家的偏移（X,Y偏移，Z固定）
    public float smoothSpeed = 0.125f; // 平滑跟随速度

    void Start()
    {
        // 如果未手动指定玩家，则通过标签查找
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogError("未找到玩家对象，请确保玩家有 'Player' 标签");
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 目标位置：玩家位置 + 偏移，但只修改X和Y，保持相机的Z值不变
        Vector3 desiredPosition = new Vector3(player.position.x + offset.x,
                                               player.position.y + offset.y,
                                               transform.position.z); // 保持相机原来的Z

        // 平滑移动
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}