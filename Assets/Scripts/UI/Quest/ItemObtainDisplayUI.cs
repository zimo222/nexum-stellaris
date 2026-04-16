using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品获得展示UI组件，负责逐个展示奖励物品、自动淡出销毁
/// </summary>
public class ItemObtainDisplayUI : MonoBehaviour
{
    public static ItemObtainDisplayUI Instance { get; private set; }

    [Header("UI容器")]
    public Transform container;
    public GameObject itemPrefab;

    [Header("时间设置")]
    public float spawnInterval = 0.5f;
    public float lifeTime = 3f;
    public float fadeDuration = 0.5f;

    private Queue<string> pendingIds = new Queue<string>();
    private bool isProcessing = false;
    private List<ItemObtainDisplayItem> activeItems = new List<ItemObtainDisplayItem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (container == null)
            Debug.LogError("ItemObtainDisplayUI: container未设置！");
        if (itemPrefab == null)
            Debug.LogError("ItemObtainDisplayUI: itemPrefab未设置！");

        if (container != null)
            container.gameObject.SetActive(false);
    }

    public void ShowItemRewards(List<string> itemIds)
    {
        if (itemIds == null || itemIds.Count == 0) return;
        foreach (string id in itemIds)
            pendingIds.Enqueue(id);
        if (!isProcessing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;
        while (pendingIds.Count > 0)
        {
            string itemId = pendingIds.Dequeue();
            CreateAndInsertItem(itemId);
            yield return new WaitForSeconds(spawnInterval);
        }
        isProcessing = false;
    }

    private void CreateAndInsertItem(string itemId)
    {
        if (!container.gameObject.activeSelf)
            container.gameObject.SetActive(true);

        Sprite icon = GetItemIcon(itemId);
        string itemName = GetItemName(itemId);
        string itemType = GetItemTypeText(itemId);

        if (string.IsNullOrEmpty(itemName))
        {
            Debug.LogWarning($"未找到物品ID对应的数据: {itemId}");
            return;
        }

        GameObject newItemObj = Instantiate(itemPrefab, container);
        ItemObtainDisplayItem displayItem = newItemObj.GetComponent<ItemObtainDisplayItem>();
        if (displayItem == null)
        {
            Debug.LogError("预制体上缺少ItemObtainDisplayItem组件");
            Destroy(newItemObj);
            return;
        }

        displayItem.Initialize(icon, itemName, itemType, lifeTime, fadeDuration, OnItemDestroyed);
        displayItem.transform.SetAsFirstSibling();
        activeItems.Add(displayItem);
    }

    private void OnItemDestroyed(ItemObtainDisplayItem item)
    {
        activeItems.Remove(item);
        if (activeItems.Count == 0 && container != null)
            container.gameObject.SetActive(false);
    }

    // ----- 数据获取（与GameDataManager集成）-----
    private Sprite GetItemIcon(string id)
    {
        var dataMgr = GameDataManager.Instance;
        if (dataMgr.ExotextDict.TryGetValue(id, out var exotext))
            return exotext.icon;
        if (dataMgr.NexusVestureDict.TryGetValue(id, out var vesture))
            return vesture.icon;
        if (dataMgr.MaterialDict.TryGetValue(id, out var material))
            return material.icon;
        return null;
    }

    private string GetItemName(string id)
    {
        var dataMgr = GameDataManager.Instance;
        if (dataMgr.ExotextDict.TryGetValue(id, out var exotext))
            return LocalizationManager.Instance.GetText("Exotext_Name", exotext.id) ?? "";
        if (dataMgr.NexusVestureDict.TryGetValue(id, out var vesture))
            return LocalizationManager.Instance.GetText("NexusVesture_Name", vesture.id) ?? "";
        if (dataMgr.MaterialDict.TryGetValue(id, out var material))
            return material.materialName;
        return null;
    }

    private string GetItemTypeText(string id)
    {
        if (GameDataManager.Instance.ExotextDict.ContainsKey(id))
            return "绎语";
        if (GameDataManager.Instance.NexusVestureDict.ContainsKey(id))
            return "络身";
        if (GameDataManager.Instance.MaterialDict.ContainsKey(id))
            return "材料";
        return "物品";
    }
}