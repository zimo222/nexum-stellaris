using UnityEngine;
using static Cinemachine.DocumentationSortingAttribute;

public class TestShortcuts : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int level = PlayerDataManager.Instance.CurrentPlayerData.Level;
            PlayerDataManager.Instance.AddExperience(50 * level * level + 150 * level + 200);
        }
        
        if (Input.GetKeyDown(KeyCode.W))
        {
            PlayerDataManager.Instance.CurrentPlayerData.Level = 1;
        }
        /*
        if (Input.GetKeyDown(KeyCode.E))
        {
            // TODO: 写你的 E 功能
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // TODO: 写你的 R 功能
        }
        */
        // 需要更多按键？复制上面的 if 块改一下 KeyCode 就行
    }
}