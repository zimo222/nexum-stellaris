using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum SpellModuleType
{
    Split,      // 分裂
    Burst,      // 爆裂（多发）
    Rotate,     // 旋转
    Homing,     // 追踪
    SpeedUp,    // 加速
    // 后续可扩展
}

[CreateAssetMenu(fileName = "NewSpellModule", menuName = "GameData/SpellModule")]
public class SpellModuleSO : ScriptableObject
{
    public string moduleId;                 // 唯一ID
    public SpellModuleType moduleType;       // 类型
    public string moduleName;                // 显示名称
    public Sprite icon;                      // UI图标
    [TextArea] public string description;    // 描述

    // 模块参数（根据类型使用不同字段，可后续扩展为模块参数类）
    public int splitCount = 2;                // 分裂数量
    public float splitAngle = 30f;             // 分裂角度
    public int burstCount = 3;                 // 爆裂数量
    public float burstDelay = 0.1f;            // 爆裂间隔
    public float rotateSpeed = 180f;            // 旋转速度（度/秒）
    public float homingStrength = 5f;           // 追踪强度
    public float speedMultiplier = 2f;          // 速度倍率
    public float orbitRadius = 10f;             //旋转半径
    public float radialSpeed = 0;               //径向增长速率
}
