using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HotReferenceManager : Singleton<HotReferenceManager>
{
    protected override void Awake()
    {
        base.Awake();

        if (gameObject.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    // 在场景加载后调用，刷新所有 QuestTriggerZone 的按钮引用
    public void RefreshAllQuestTriggerZones()
    {
        // 使用 FindObjectsOfType 包括未激活的物体（但注意：FindObjectsOfType 默认只返回激活的，要包括未激活需要 Resources.FindObjectsOfTypeAll 并过滤）
        QuestTriggerZone[] zones = Resources.FindObjectsOfTypeAll<QuestTriggerZone>();
        List<QuestTriggerZone> validZones = new List<QuestTriggerZone>();

        foreach (var zone in zones)
        {
            // 只保留属于当前场景且未被销毁的实例
            if (zone != null && zone.gameObject.scene == SceneManager.GetActiveScene())
            {
                validZones.Add(zone);
            }
        }

        Debug.Log($"HotReferenceManager: 找到 {validZones.Count} 个 QuestTriggerZone，开始刷新按钮引用...");

        foreach (var zone in validZones)
        {
            zone.RefreshButtonReference();
        }
    }

    /// <summary>
    /// 将当前场景中找到的 WeaponSlotsUI 重新赋值给 Player 实例
    /// </summary>
    public void RefreshPlayerWeaponSlotsUI()
    {
        if (Player.Instance == null)
        {
            Debug.LogWarning("HotReferenceManager: Player.Instance 为空，无法刷新 WeaponSlotsUI");
            return;
        }

        // 查找场景中的 WeaponSlotsUI（包括未激活的物体）
        WeaponSlotsUI slotsUI = FindObjectOfType<WeaponSlotsUI>(true);
        if (slotsUI != null)
        {
            Player.Instance.weaponSlotsUI = slotsUI;
            Debug.Log("HotReferenceManager: 已刷新 Player 的 WeaponSlotsUI 引用");
        }
        else
        {
            Debug.LogWarning("HotReferenceManager: 未在当前场景中找到 WeaponSlotsUI");
        }
    }
}