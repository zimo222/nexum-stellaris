using UnityEngine;

public class FollowPlayer2D : MonoBehaviour
{
    public Vector3 offset = new Vector3(0, 0, 0);
    public float smoothSpeed = 0.125f;

    private void Awake()
    {
        

    }

    private void Start()
    {
        

    }

    void Update()
    {
        // 通过单例获取当前唯一存活的玩家
        if (Player.Instance == null) return;

        Transform player = Player.Instance.transform;

        Vector3 desiredPosition = new Vector3(player.position.x + offset.x,
                                               player.position.y + offset.y,
                                               transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}