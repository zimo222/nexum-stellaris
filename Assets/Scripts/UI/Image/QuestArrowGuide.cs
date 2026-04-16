using UnityEngine;

public class QuestArrowGuide : MonoBehaviour
{
    [Header("箭头UI")]
    public RectTransform arrowImage;          // 箭头的 RectTransform
    public float radius = 150f;               // 箭头旋转半径（像素）

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

        // 动态计算半径：取屏幕较短边的 1/3，保证箭头在屏幕边缘内
        radius = Mathf.Min(Screen.width, Screen.height) * 0.3f;

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

        // 查找 QuestTriggerZone
        QuestTriggerZone[] questZones = FindObjectsOfType<QuestTriggerZone>();
        foreach (QuestTriggerZone zone in questZones)
        {
            if (zone.questId == questId)
            {
                targetTransform = zone.transform;
                break;
            }
        }

        // 如果没找到，再查找 CombatQuestTrigger
        if (targetTransform == null)
        {
            CombatQuestTrigger[] combatZones = FindObjectsOfType<CombatQuestTrigger>();
            foreach (CombatQuestTrigger zone in combatZones)
            {
                if (zone.questId == questId)
                {
                    targetTransform = zone.transform;
                    break;
                }
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

        // 计算指向目标的方向向量（忽略 Z 轴）
        Vector3 dir3 = targetTransform.position - playerTransform.position;
        dir3.z = 0;
        if (dir3 == Vector3.zero) return;

        Vector2 direction = new Vector2(dir3.x, dir3.y).normalized;

        // 计算角度（弧度 → 度数）
        float angleRad = Mathf.Atan2(direction.y, direction.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        // 圆形边界上的偏移（像素）
        Vector2 circleOffset = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * radius;

        // 屏幕中心
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // 箭头在屏幕上的像素位置
        Vector2 targetScreenPos = screenCenter + circleOffset;

        // 边界裁剪：确保箭头不超出屏幕
        targetScreenPos.x = Mathf.Clamp(targetScreenPos.x, 0, Screen.width);
        targetScreenPos.y = Mathf.Clamp(targetScreenPos.y, 0, Screen.height);

        // 将屏幕坐标转换为 UI 局部坐标（因为 Canvas 是 Overlay 且锚点居中）
        Vector2 localPos = targetScreenPos - screenCenter;
        arrowImage.anchoredPosition = localPos;

        // 旋转箭头：让箭头指向目标方向
        // 如果你的箭头图片默认朝右（→），使用 angleDeg；默认朝下（↓）则减90°
        arrowImage.rotation = Quaternion.Euler(0, 0, angleDeg + 90f);
    }
}