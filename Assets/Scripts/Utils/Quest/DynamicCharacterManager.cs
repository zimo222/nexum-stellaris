using System.Collections.Generic;
using UnityEngine;

public class DynamicCharacterManager : MonoBehaviour
{
    public static DynamicCharacterManager Instance { get; private set; }

    private Dictionary<string, GameObject> spawnedCharacters = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 生成角色
    /// </summary>
    public GameObject SpawnCharacter(SpawnCharacterData data)
    {
        if (data.prefab == null)
        {
            Debug.LogError($"生成角色失败：预制体为空 (ID: {data.characterId})");
            return null;
        }

        if (spawnedCharacters.ContainsKey(data.characterId))
        {
            Debug.LogWarning($"角色 {data.characterId} 已存在，将先销毁再生成");
            DestroyCharacter(data.characterId);
        }

        GameObject newChar = Instantiate(data.prefab, data.spawnPosition, Quaternion.identity);
        // 确保有 NPCIdentifier 组件（用于后续移动查找）
        var identifier = newChar.GetComponent<NPCIdentifier>();
        if (identifier == null)
            identifier = newChar.AddComponent<NPCIdentifier>();
        identifier.speakerId = data.characterId;

        // 设置初始动画
        var animator = newChar.GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(data.startState))
            animator.Play(data.startState);

        spawnedCharacters[data.characterId] = newChar;
        Debug.Log($"生成角色: {data.characterId} 于 {data.spawnPosition}");
        return newChar;
    }

    /// <summary>
    /// 销毁角色
    /// </summary>
    public bool DestroyCharacter(string characterId)
    {
        if (spawnedCharacters.TryGetValue(characterId, out GameObject go))
        {
            if (go != null) Destroy(go);
            spawnedCharacters.Remove(characterId);
            Debug.Log($"销毁角色: {characterId}");
            return true;
        }
        Debug.LogWarning($"未找到动态角色: {characterId}");
        return false;
    }

    /// <summary>
    /// 根据标识获取角色 GameObject（供移动使用）
    /// </summary>
    public GameObject GetCharacter(string characterId)
    {
        if (spawnedCharacters.TryGetValue(characterId, out GameObject go))
            return go;
        return null;
    }

    /// <summary>
    /// 清空所有动态生成的角色
    /// </summary>
    public void ClearAllDynamicCharacters()
    {
        foreach (var go in spawnedCharacters.Values)
            if (go != null) Destroy(go);
        spawnedCharacters.Clear();
        Debug.Log("清空所有动态角色");
    }
}