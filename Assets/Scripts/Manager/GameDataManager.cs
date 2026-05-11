using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    public Dictionary<string, ExotextDefineSO> ExotextDict { get; private set; }
    public Dictionary<string, NexusVestureDefineSO> NexusVestureDict { get; private set; }
    public Dictionary<string, MaterialDefineSO> MaterialDict { get; private set; }
    public Dictionary<string, QuestDefineSO> QuestDict { get; private set; }
    public Dictionary<string, BulletDefineSO> BulletDict { get; private set; }
    public Dictionary<string, SpellModuleSO> SpellModuleDict { get; private set; }
    public Dictionary<string, TutorialDefineSO> TutorialDict { get; private set; }
    public Dictionary<string, SpecialEffectDefineSO> SpecialEffectDict { get; private set; }

    void Awake()
    {
        DeadlockDetector.Log($"[{GetType().Name}] Awake on {gameObject.name}");
        // 实现简单的单例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        /*
        ExotextDefineSO[] exotexts = Resources.LoadAll<ExotextDefineSO>("GameData/Exotext");
        ExotextDict = exotexts.ToDictionary(w => w.id, w => w);

        NexusVestureDefineSO[] nexusvestures = Resources.LoadAll<NexusVestureDefineSO>("GameData/NexusVesture");
        NexusVestureDict = nexusvestures.ToDictionary(w => w.id, w => w);

        MaterialDefineSO[] materials = Resources.LoadAll<MaterialDefineSO>("GameData/Material");
        MaterialDict = materials.ToDictionary(w => w.id, w => w);

        QuestDefineSO[] quests = Resources.LoadAll<QuestDefineSO>("GameData/Quest");
        QuestDict = quests.ToDictionary(w => w.id, w => w);

        BulletDefineSO[] bullets = Resources.LoadAll<BulletDefineSO>("GameData/Bullet");
        BulletDict = bullets.ToDictionary(b => b.id, b => b);

        SpellModuleSO[] modules = Resources.LoadAll<SpellModuleSO>("GameData/SpellModule");
        SpellModuleDict = modules.ToDictionary(m => m.id, m => m);

        TutorialDefineSO[] tutorials = Resources.LoadAll<TutorialDefineSO>("GameData/Tutorial");
        TutorialDict = tutorials.ToDictionary(t => t.sequenceName, t => t);
        */
        ExotextDict = LoadDict<ExotextDefineSO>("GameData/Exotext");
        NexusVestureDict = LoadDict<NexusVestureDefineSO>("GameData/NexusVesture");
        MaterialDict = LoadDict<MaterialDefineSO>("GameData/Material");
        QuestDict = LoadDict<QuestDefineSO>("GameData/Quest");
        BulletDict = LoadDict<BulletDefineSO>("GameData/Bullet");
        SpellModuleDict = LoadDict<SpellModuleSO>("GameData/SpellModule");
        TutorialDict = LoadDict<TutorialDefineSO>("GameData/Tutorial");
        SpecialEffectDict = LoadDict<SpecialEffectDefineSO>("GameData/SpecialEffect");
    }
    // 通用加载方法，避免重复代码
    private Dictionary<string, T> LoadDict<T>(string folder) where T : Object
    {
        T[] arr = Resources.LoadAll<T>(folder);
        return arr.ToDictionary(item => GetId(item));
    }

    // 根据不同类型获取id字段（因为 ScriptableObject 没有统一id字段）
    private string GetId<T>(T obj)
    {
        if (obj is ExotextDefineSO e) return e.id;
        if (obj is NexusVestureDefineSO n) return n.id;
        if (obj is MaterialDefineSO m) return m.id;
        if (obj is QuestDefineSO q) return q.id;
        if (obj is BulletDefineSO b) return b.id;
        if (obj is SpellModuleSO s) return s.id;
        if (obj is TutorialDefineSO t) return t.sequenceName;
        if (obj is SpecialEffectDefineSO se) return se.id;
        return null;
    }
}