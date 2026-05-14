using UnityEngine;

/// <summary>
/// 挂载到 Tilemap 对象上（需有 TilemapCollider2D 且勾选 Is Trigger）。
/// 当标签为 Player 的对象进入触发器时隐藏一组物体，离开时显示。
/// </summary>
public class TilemapHideObjects : MonoBehaviour
{
    [Header("要控制显示/隐藏的物体")]
    [Tooltip("将这些物体拖入列表")]
    public GameObject[] objectsToHide;

    [Header("检测设置")]
    [Tooltip("玩家的标签，默认为 Player")]
    public string playerTag = "Player";

    // 用于记录当前触发器内玩家数量（支持多个玩家同时进入）
    private int playersInside = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检测是否是玩家
        if (other.CompareTag(playerTag))
        {
            playersInside++;
            // 只有第一个玩家进入时才隐藏物体（避免重复调用）
            if (playersInside == 1)
            {
                SetObjectsActive(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playersInside--;
            // 所有玩家都离开后再显示物体
            if (playersInside <= 0)
            {
                playersInside = 0; // 防止负数
                SetObjectsActive(true);
            }
        }
    }

    /// <summary>
    /// 批量设置物体的激活状态
    /// </summary>
    /// <param name="active">true 显示，false 隐藏</param>
    private void SetObjectsActive(bool active)
    {
        foreach (GameObject obj in objectsToHide)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}