using UnityEngine;
//子弹静态数据
[CreateAssetMenu(fileName = "NewBullet", menuName = "GameData/BulletDefine")]
public class BulletDefineSO : ScriptableObject
{
    public string id;               // 子弹唯一ID
    public float speed = 10f;        // 子弹飞行速度
    public double damage = 10;          // 子弹基础伤害（实际伤害会叠加角色攻击力）
    public GameObject prefab;        // 子弹预制体
    public float lifetime = 2f;      // 子弹自动销毁时间
    // 可扩展：击中特效、音效等
}