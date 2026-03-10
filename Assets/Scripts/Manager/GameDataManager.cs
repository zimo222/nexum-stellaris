using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }
    public Dictionary<string, ExotextDefineSO> ExotextDict { get; private set; }
    public Dictionary<string, NexusVestureDefineSO> NexusVestureDict { get; private set; }
    public Dictionary<string, MaterialDefineSO> MaterialDict { get; private set; }
    public Dictionary<string, QuestDefineSO> QuestDict { get; private set;}

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
        }

        ExotextDefineSO[] exotexts = Resources.LoadAll<ExotextDefineSO>("GameData/Exotext");
        ExotextDict = exotexts.ToDictionary(w => w.id, w => w);

        NexusVestureDefineSO[] nexusvestures = Resources.LoadAll<NexusVestureDefineSO>("GameData/NexusVesture");
        NexusVestureDict = nexusvestures.ToDictionary(w => w.id, w => w);

        MaterialDefineSO[] materials = Resources.LoadAll<MaterialDefineSO>("GameData/Material");
        MaterialDict = materials.ToDictionary(w => w.id, w => w);

        QuestDefineSO[] quests = Resources.LoadAll<QuestDefineSO>("GameData/Quest");
        Debug.Log(quests[0]);
        QuestDict = quests.ToDictionary(w => w.id, w => w);
    }
}