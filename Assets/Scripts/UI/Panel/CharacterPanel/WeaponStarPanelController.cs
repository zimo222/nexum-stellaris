using System.Collections.Generic;
using UnityEngine;

public class WeaponStarPanelController : MonoBehaviour
{
    [Header("视图组件")]
    [SerializeField] private WeaponStarView[] starViews;        // 场景中所有星辰（手动拖拽）
    [SerializeField] private WeaponDetailView detailView;       // 详情面板

    private PlayerDataManager playerDataManager;

    void Start()
    {
        playerDataManager = PlayerDataManager.Instance;
        if (playerDataManager == null)
        {
            Debug.LogError("PlayerDataManager 未找到");
            return;
        }

        // 初始化所有星辰视图
        foreach (var star in starViews)
        {
            star.Initialize(this);
        }

        // 初始化详情面板
        if (detailView != null)
            detailView.Initialize(this);

        // 监听玩家数据变化
        playerDataManager.OnPlayerDataChanged += OnPlayerDataChanged;

        // 初始刷新
        RefreshAllStars();

        this.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerDataManager != null)
            playerDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
    }

    /// <summary> 刷新所有星辰的状态 </summary>
    public void RefreshAllStars()
    {
        if (playerDataManager == null) return;

        foreach (var star in starViews)
        {
            var weapon = playerDataManager.GetExotextByDefineId(star.weaponDefineId);
            bool unlocked = weapon != null;
            bool isEquipped = false;

            if (unlocked)
            {
                // 检查该武器是否被装备
                var equippedId = playerDataManager.GetEquippedExotextId(weapon.Type);
                isEquipped = (equippedId == weapon.Id);
            }

            star.UpdateState(unlocked, isEquipped);
        }
    }

    /// <summary> 处理星辰点击 </summary>
    public void OnStarClicked(WeaponStarView star)
    {
        if (playerDataManager == null) return;

        var weapon = playerDataManager.GetExotextByDefineId(star.weaponDefineId);
        if (weapon == null)
        {
            // 未解锁：可显示提示（如“尚未获得该武器”）
            Debug.Log("武器未解锁");
            return;
        }

        detailView.Show(weapon);
    }

    /// <summary> 装备武器（由详情面板调用）</summary>
    public void EquipWeapon(string defineId)
    {
        if (playerDataManager == null) return;
        playerDataManager.EquipExotext(defineId);
        // 数据变化后会自动刷新所有星辰
    }

    /// <summary> 数据变化回调 </summary>
    private void OnPlayerDataChanged(PlayerData data)
    {
        RefreshAllStars();
    }
}