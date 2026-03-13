using UnityEngine;

public class QuestArrowGuide : MonoBehaviour
{
    [Header("箭头UI")]
    public RectTransform arrowImage;          // 箭头的 RectTransform
    public float radius = 100f;                 // 箭头旋转半径（像素）

    [Header("追踪目标")]
    private Transform targetTransform;
    private Transform playerTransform;

    private void Start()
    {
        if (arrowImage == null)
        {
            Debug.LogError("请将箭头图片的 RectTransform 赋值给 arrowImage");
            enabled = false;
            return;
        }

        arrowImage.gameObject.SetActive(false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTrackedQuestChanged += OnTrackedQuestChanged;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogError("未找到玩家物体");
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTrackedQuestChanged -= OnTrackedQuestChanged;
    }

    private void OnTrackedQuestChanged(string questId)
    {
        targetTransform = null;

        if (string.IsNullOrEmpty(questId))
        {
            arrowImage.gameObject.SetActive(false);
            return;
        }

        QuestTriggerZone[] allZones = FindObjectsOfType<QuestTriggerZone>();
        foreach (QuestTriggerZone zone in allZones)
        {
            if (zone.questId == questId)
            {
                targetTransform = zone.transform;
                break;
            }
        }

        if (targetTransform == null)
        {
            Debug.LogWarning($"未找到任务 {questId} 对应的触发器");
            arrowImage.gameObject.SetActive(false);
        }
        else
        {
            arrowImage.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (targetTransform == null || playerTransform == null) return;

        // 计算从玩家指向目标的向量（忽略 Z 轴，假设是 2D 平面）
        Vector3 direction = targetTransform.position - playerTransform.position;
        direction.z = 0;

        if (direction == Vector3.zero) return;

        Vector2 dir = new Vector2(direction.x, direction.y).normalized;

        // 屏幕中心
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // 箭头在屏幕上的像素位置：屏幕中心 + 半径 * 方向
        Vector2 screenPos = screenCenter + dir * radius;

        // 将屏幕坐标转换为 Canvas 本地坐标
        Canvas canvas = arrowImage.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Camera uiCamera = (canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                screenPos,
                uiCamera,
                out localPos);
            arrowImage.anchoredPosition = localPos;
        }
        else
        {
            arrowImage.anchoredPosition = screenPos; // 备用
        }

        // 箭头旋转：默认指向正下方 (Vector2.down)，旋转到目标方向
        float angleDeg = Vector2.SignedAngle(Vector2.down, dir);
        arrowImage.rotation = Quaternion.Euler(0, 0, angleDeg);
    }
}