using Unity.VisualScripting;
using UnityEngine;

// 圣痕定义SO
[CreateAssetMenu(fileName = "NewNexusVesture", menuName = "GameData/NexusVestureDefine")]
public class NexusVestureDefineSO : ScriptableObject, IHaveId
{
    public string id;
    public string Id => id;
    public string nexusvectureName;

    public NexusVesturePosition Position;

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
    /*
    [TextArea] public string introduction;
    [TextArea] public string description;
    */
    /*
    [System.Serializable]
    public class stigmataSkill
    {
        public string name;
        [TextArea] public string description;
    }
    public stigmataSkill[] skill;
    public GameObject weaponPrefab;
    public Sprite icon;
    */
}
