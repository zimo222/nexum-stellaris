using System.Collections.Generic;
using UnityEngine;

public class NPCIdentifier : MonoBehaviour
{
    [SerializeField]
    private List<string> speakerIds = new List<string>();

    // 兼容旧代码：保留 speakerId 属性，但实际操作列表
    public string speakerId
    {
        get => speakerIds.Count > 0 ? speakerIds[0] : null;
        set
        {
            if (!speakerIds.Contains(value))
                speakerIds.Add(value);
        }
    }

    public List<string> SpeakerIds => speakerIds;

    public bool HasId(string id) => speakerIds.Contains(id);

    public void AddId(string id)
    {
        if (!speakerIds.Contains(id))
            speakerIds.Add(id);
    }
}