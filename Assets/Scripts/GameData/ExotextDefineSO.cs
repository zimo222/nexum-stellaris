using TMPro;
using Unity.VisualScripting;
using UnityEngine;

// Œ‰∆˜∂®“ÂSO
[CreateAssetMenu(fileName = "NewExotext", menuName = "GameData/ExotextDefine")]
public class ExotextDefineSO : ScriptableObject
{
    public string id;
    public string exotextName;

    public ExotextType type;

    public string element;

    public int baseStars;
    public int maxStars;

    public int baseHealth;
    public int baseAttack;
    public int baseDefence;


    public int baseEnergy;
    public float baseCritRate;
    public float baseCritDamage;
    public float baseElementBonus;

    public Sprite icon;
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
