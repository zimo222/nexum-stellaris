using UnityEngine;

public class QuestTriggerZone : MonoBehaviour
{
    public string questId;  // 这个触发器属于哪个任务

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            QuestManager.Instance?.OnPlayerEnterQuestArea(questId);
        }
    }
}