using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class CharacterPanelView : MonoBehaviour
{

    [Header("角色信息")]
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenceText;
    [SerializeField] private TextMeshProUGUI critRateText;
    [SerializeField] private TextMeshProUGUI critDamageText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI statsText;    // 基础属性

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerDataChanged += OnPlayerDataChanged;
            // 立即刷新一次
            OnPlayerDataChanged(PlayerDataManager.Instance.CurrentPlayerData);
        }
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayerDataChanged -= OnPlayerDataChanged;
        }
    }

    private void OnPlayerDataChanged(PlayerData newData)
    {
        UpdateUI(newData);
    }

    public void UpdateUI(PlayerData currentPlayerData)
    {

        if (nameText != null) nameText.text = currentPlayerData.PlayerName;
        if (levelText != null) levelText.text = $"Lv.{currentPlayerData.Level}";
        if (expText != null) expText.text = $"EXP: {currentPlayerData.Experience}";

        var stats = currentPlayerData.BaseStats;
        if (healthText != null) healthText.text = $"{currentPlayerData.TotalHealth}";
        if (attackText != null) attackText.text = $"{currentPlayerData.TotalAttack}";
        if (defenceText != null) defenceText.text = $"{currentPlayerData.TotalDefence}";
        if (critRateText != null) critRateText.text = $"{currentPlayerData.TotalCritRate * 100}%";
        if (critDamageText != null) critDamageText.text = $"{currentPlayerData.TotalCritDamage * 100}%";
        if (energyText != null) energyText.text = $"{currentPlayerData.TotalEnergy}%";
    }
}
