using UnityEngine;

public class LoadSceneOnKeyPress : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (SceneDataManager.Instance != null)
            {
                SceneDataManager.Instance.LoadScene("1_TheNestOfWarmLight");
            }
            else
            {
                Debug.LogError("SceneDataManager 实例不存在，无法加载场景。");
            }
        }
    }
}