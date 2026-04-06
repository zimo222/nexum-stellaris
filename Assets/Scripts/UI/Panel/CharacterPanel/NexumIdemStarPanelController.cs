using UnityEngine;
using UnityEngine.UI;

public class NexumIdemStarPanelController : MonoBehaviour
{
    public enum NexumIdemMode { Exotext, NexusVesture }

    [Header("模式选择（二选一）")]
    [SerializeField] private NexumIdemMode mode = NexumIdemMode.Exotext;

    [Header("武器星辰列表（仅在模式为武器时拖入）")]
    [SerializeField] private NexumIdemStarView[] exotextStarViews;

    [Header("防具星辰列表（仅在模式为防具时拖入）")]
    [SerializeField] private NexumIdemStarView[] nexusVestureStarViews;

    [Header("详情面板")]
    [SerializeField] private NexumIdemDetailView detailView;

    [Header("可选：模式切换按钮（运行时）")]
    public Button switchModeButton;   // 如果不需要运行时切换可以不拖
    public Text modeText;

    private PlayerDataManager playerDataManager;
    private NexumIdemMode currentMode;  // 运行时当前模式（可能与初始 mode 一致）

    void Start()
    {
        playerDataManager = PlayerDataManager.Instance;
        if (playerDataManager == null)
        {
            Debug.LogError("PlayerDataManager 未找到");
            return;
        }

        // 设置初始模式（使用 Inspector 中选择的 mode）
        currentMode = mode;

        // 根据模式初始化对应的星辰列表
        var targetStars = (currentMode == NexumIdemMode.Exotext) ? exotextStarViews : nexusVestureStarViews;
        foreach (var star in targetStars)
        {
            if (star != null)
                star.Initialize(this, currentMode);
        }

        if (detailView != null)
            detailView.Initialize(this);

        if (switchModeButton != null)
            switchModeButton.onClick.AddListener(SwitchMode);

        playerDataManager.OnPlayerDataChanged += OnPlayerDataChanged;

        RefreshByMode();
    }

    private void OnDestroy()
    {
        if (playerDataManager != null)
            playerDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
    }

    // 运行时切换模式（可选，如果不需要可删除此方法及按钮）
    private void SwitchMode()
    {
        currentMode = (currentMode == NexumIdemMode.Exotext) ? NexumIdemMode.NexusVesture : NexumIdemMode.Exotext;
        RefreshByMode();
        if (modeText != null)
            modeText.text = currentMode == NexumIdemMode.Exotext ? "武器" : "防具";
    }

    private void RefreshByMode()
    {
        // 根据当前模式显示对应的星辰组（隐藏另一组）
        foreach (var star in exotextStarViews)
            if (star != null) star.gameObject.SetActive(currentMode == NexumIdemMode.Exotext);
        foreach (var star in nexusVestureStarViews)
            if (star != null) star.gameObject.SetActive(currentMode == NexumIdemMode.NexusVesture);

        RefreshAllStars();
    }

    public void RefreshAllStars()
    {
        if (playerDataManager == null) return;

        if (currentMode == NexumIdemMode.Exotext)
        {
            foreach (var star in exotextStarViews)
            {
                if (star == null) continue;
                var weapon = playerDataManager.GetExotextByDefineId(star.ItemDefineId);
                bool unlocked = weapon != null;
                bool isEquipped = unlocked && playerDataManager.GetEquippedExotextId(weapon.Type) == weapon.Id;
                star.UpdateState(unlocked, isEquipped);
            }
        }
        else
        {
            foreach (var star in nexusVestureStarViews)
            {
                if (star == null) continue;
                var vesture = playerDataManager.GetNexusVestureByDefineId(star.ItemDefineId);
                bool unlocked = vesture != null;
                bool isEquipped = unlocked && playerDataManager.GetEquippedNexusVestureId(vesture.Position) == vesture.Id;
                star.UpdateState(unlocked, isEquipped);
            }
        }
    }

    public void OnStarClicked(NexumIdemStarView star)
    {
        if (playerDataManager == null) return;

        if (currentMode == NexumIdemMode.Exotext)
        {
            var weapon = playerDataManager.GetExotextByDefineId(star.ItemDefineId);
            if (weapon == null)
            {
                Debug.Log("武器未解锁");
                return;
            }
            detailView.Show(weapon);
        }
        else
        {
            var vesture = playerDataManager.GetNexusVestureByDefineId(star.ItemDefineId);
            if (vesture == null)
            {
                Debug.Log("防具未解锁");
                return;
            }
            detailView.Show(vesture);
        }
    }

    public void EquipNexumIdem(string defineId)
    {
        if (playerDataManager == null) return;
        if (defineId[0] == 'E')
            playerDataManager.EquipExotext(defineId);
        else
            playerDataManager.EquipNexusVesture(defineId);
    }

    private void OnPlayerDataChanged(PlayerData data)
    {
        RefreshAllStars();
    }
}