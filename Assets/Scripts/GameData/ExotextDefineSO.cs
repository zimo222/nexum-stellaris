using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

// 武器定义SO
[CreateAssetMenu(fileName = "NewExotext", menuName = "GameData/ExotextDefine")]
public class ExotextDefineSO : ScriptableObject
{
    public string id;
    public string exotextName;

    public ExotextType type;

    public string element;

    public int baseStars;
    public int maxStars;

    public float baseHealth;
    public float healthPerLevel;
    public float baseAttack;
    public float attackPerLevel;
    public float baseDefence;
    public float defencePerLevel;

    public int baseEnergy;
    public int energyPerLevel;

    public float baseCritRate;    // 原 baseCritRate，心弦率基础
    public float critRatePerLevel;
    public float baseCritDamage;         // 原 baseCritDamage，绎动值基础
    public float critDamagePerLevel;
    public float baseElementBonus;
    public float elementBonusPerLevel;

    public Sprite icon;

    // ========== 新增 ==========
    [Header("特殊效果（心弦/绎动）")]
    public List<SpecialEffectDefineSO> possibleEffects;  // 该武器可能触发的特效列表


    [Header("可用模块")]
    public List<SpellModuleSO> spellModuleSOs;
    /*
    [TextArea] public string introduction;
    [TextArea] public string description;
    */

    /*
    [System.Serializable]
    public class weaponSkill
    {
        public string name;
        [TextArea] public string description;
    }
    public weaponSkill[] skill;
    public GameObject weaponPrefab;
    public Sprite icon;
    */
}
