using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestCommandExecutor : MonoBehaviour
{
    public IEnumerator ExecuteCommand(QuestCommand command)
    {
        switch (command.commandType)
        {
            case CommandType.MoveCharacters:
                yield return ExecuteMovements(command.movements);
                break;
            case CommandType.Wait:
                yield return new WaitForSeconds(command.waitTime);
                break;
            case CommandType.SpawnCharacter:
                ExecuteSpawn(command.spawnData);
                break;
            case CommandType.DestroyCharacter:
                ExecuteDestroy(command.destroyData);
                break;
            default:
                Debug.LogWarning($"未实现的指令类型: {command.commandType}");
                yield break;
        }
    }

    private void ExecuteSpawn(SpawnCharacterData data)
    {
        if (DynamicCharacterManager.Instance != null)
            DynamicCharacterManager.Instance.SpawnCharacter(data);
        else
            Debug.LogError("DynamicCharacterManager 不存在，无法生成角色");
    }

    private void ExecuteDestroy(DestroyCharacterData data)
    {
        if (DynamicCharacterManager.Instance != null)
            DynamicCharacterManager.Instance.DestroyCharacter(data.characterId);
        else
            Debug.LogError("DynamicCharacterManager 不存在，无法销毁角色");
    }

    private IEnumerator ExecuteMovements(List<CharacterMovement> movements)
    {
        if (movements == null || movements.Count == 0) yield break;

        List<Coroutine> waitingCoroutines = new List<Coroutine>();

        foreach (var mv in movements)
        {
            GameObject target = FindCharacter(mv.characterId);
            if (target == null)
            {
                Debug.LogWarning($"找不到角色: {mv.characterId}");
                continue;
            }

            CharacterMover mover = target.GetComponent<CharacterMover>();
            if (mover == null) mover = target.AddComponent<CharacterMover>();

            Coroutine cor = StartCoroutine(mover.MoveAlongPath(mv.waypoints, mv.speed));
            if (mv.waitForCompletion)
                waitingCoroutines.Add(cor);
        }

        foreach (var cor in waitingCoroutines)
            yield return cor;
    }

    private GameObject FindCharacter(string characterId)
    {
        // 先查动态管理器
        if (DynamicCharacterManager.Instance != null)
        {
            GameObject go = DynamicCharacterManager.Instance.GetCharacter(characterId);
            if (go != null) return go;
        }

        // 再查静态 NPC
        var npcs = FindObjectsOfType<NPCIdentifier>();
        foreach (var npc in npcs)
            if (npc.speakerId == characterId)
                return npc.gameObject;

        if (characterId == "Player")
            return GameObject.FindGameObjectWithTag("Player");

        return null;
    }
}