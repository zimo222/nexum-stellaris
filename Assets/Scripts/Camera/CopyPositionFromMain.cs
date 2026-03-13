using UnityEngine;

public class CopyPositionFromMain : MonoBehaviour
{
    [SerializeField]
    public Transform mainCamera; // 将主相机拖入此字段

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // 仅复制位置，保留 Size 和 Rotation
            transform.position = mainCamera.position;
        }
    }
}