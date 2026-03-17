using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySpawnInfo
{
    public GameObject enemyPrefab;   // 敌人预制体（必须挂载 Enemy 脚本）
    public int count = 1;             // 该种类敌人的数量
    // 可扩展：生成位置偏移、延迟等
}

[Serializable]
public class WaveDefine
{
    public string waveName = "第一波";
    public List<EnemySpawnInfo> enemies = new List<EnemySpawnInfo>();
    // 可扩展：波次触发条件（如时间、全部击杀后触发下一波，默认全部击杀后自动下一波）
}