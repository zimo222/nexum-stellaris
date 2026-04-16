using UnityEngine;

public class RoofController : MonoBehaviour
{
    [Header("要控制的房顶对象")]
    public GameObject[] roof;  // 在 Inspector 中将房子的房顶对象拖拽到这里

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 判断进入触发器的对象是否是玩家（建议给玩家设置 "Player" 标签）
        if (other.CompareTag("Player"))
        {
            roof[0].SetActive(false);  // 隐藏房顶
            roof[1].SetActive(false);  // 隐藏房顶

        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            roof[0].SetActive(true);   // 显示房顶
            roof[1].SetActive(true);   // 显示房顶
        }
    }
}