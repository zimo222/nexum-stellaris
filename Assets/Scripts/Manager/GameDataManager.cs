using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    public Dictionary<string, ExotextDefineSO> ExotextDict { get; private set; }
    public Dictionary<string, NexusVestureDefineSO> NexusVestureDict { get; private set; }
    public Dictionary<string, MaterialDefineSO> MaterialDict { get; private set; }

    void Awake()
    {
        // 实现简单的单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);

            ExotextDefineSO[] weapons = Resources.LoadAll<ExotextDefineSO>("GameData/Weapons");
            ExotextDict = weapons.ToDictionary(w => w.id, w => w);

            NexusVestureDefineSO[] stigmatas = Resources.LoadAll<NexusVestureDefineSO>("GameData/Stigmatas");
            NexusVestureDict = stigmatas.ToDictionary(w => w.id, w => w);

            MaterialDefineSO[] materials = Resources.LoadAll<MaterialDefineSO>("GameData/Materials");
            MaterialDict = materials.ToDictionary(w => w.id, w => w);
        }
    }
}