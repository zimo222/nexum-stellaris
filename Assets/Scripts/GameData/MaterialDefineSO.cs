using Unity.VisualScripting;
using UnityEngine;

// ²ÄÁÏ¶¨ÒåSO
[CreateAssetMenu(fileName = "NewMaterial", menuName = "GameData/MaterialDefine")]
public class MaterialDefineSO : ScriptableObject, IHaveId
{
    public string id;
    public string Id => id;
    public string materialName;

    public int baseStars;
    public int num;

    public Sprite icon;

    [TextArea] public string introduction;
    [TextArea] public string description;
    /*
    public GameObject weaponPrefab;
    public Sprite icon;
    */
}