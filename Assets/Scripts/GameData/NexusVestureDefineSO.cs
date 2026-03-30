using Unity.VisualScripting;
using UnityEngine;

//  •∫€∂®“ÂSO
[CreateAssetMenu(fileName = "NewNexusVesture", menuName = "GameData/NexusVestureDefine")]
public class NexusVestureDefineSO : ScriptableObject
{
    public string id;
    public string nexusvectureName;

    public NexusVesturePosition Position;

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
