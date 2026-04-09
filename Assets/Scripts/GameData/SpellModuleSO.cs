// SpellModuleSO.cs
using UnityEngine;

// ModuleCategory.cs
public enum SpellModuleType
{
    Projectile,   // 投射类
    Modifier,     // 修饰类（放在投射类前面，初始化时生效）
    Corrector,    // 修正类（放在投射类后面，按延迟执行）
    MultiCast     // 多重释放类
}

[CreateAssetMenu(fileName = "NewSpellModule", menuName = "GameData/SpellModule")]
public class SpellModuleSO : ScriptableObject
{
    public string id;
    public string moduleName;
    public SpellModuleType moduleType;

    // 通用参数
    public float delay = 0f;        // 修正类的执行延迟（从子弹生成开始计时）

    // 投射类参数（引用子弹定义）
    public BulletDefineSO bulletDefine;

    // 修饰/修正参数
    public float speedMultiplier = 1f;      // 速度倍率
    public float damageMultiplier = 1f;     // 伤害倍率
    public float homingStrength = 0f;       // 追踪强度（0-1）
    public float rotateSpeed = 0f;          // 旋转速度（度/秒）
    public int splitCount = 0;               // 分裂数量
    public float splitAngle = 30f;           // 分裂角度范围（总角度）
    public int burstCount = 0;                // 爆裂次数
    public float burstDelay = 0.1f;           // 爆裂间隔

    // 多重释放参数
    public int multicastCount = 1;            // 多重释放数量

    // 轨道参数（用于旋转修正）
    public float orbitRadius = 0f;            // 轨道半径（>0 启用圆周运动）
    public float orbitSpeed = 0f;              // 轨道旋转速度

    public int fieldAngle = 0;

    public string introduction;

    public Sprite icon;
}