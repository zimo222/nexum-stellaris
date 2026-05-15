using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestControlExecutor : MonoBehaviour
{
    private Dictionary<string, GameObject> dynamicCharacters = new Dictionary<string, GameObject>();
    private Dictionary<string, Coroutine> currentMoveCoroutines = new Dictionary<string, Coroutine>();

    public IEnumerator ExecuteControls(List<QuestControl> controls)
    {
        if (controls == null || controls.Count == 0) yield break;

        List<Coroutine> routines = new List<Coroutine>();
        foreach (var ctrl in controls)
            routines.Add(StartCoroutine(ExecuteSingleControl(ctrl)));

        foreach (var routine in routines)
            yield return routine;
    }

    private IEnumerator ExecuteSingleControl(QuestControl ctrl)
    {
        if (ctrl.delay > 0)
            yield return new WaitForSeconds(ctrl.delay);

        switch (ctrl.type)
        {
            case ControlType.Spawn:
                SpawnCharacter(ctrl);
                break;
            case ControlType.Destroy:
                DestroyCharacter(ctrl.characterId);
                break;
            case ControlType.Move:
                yield return EnqueueMove(ctrl);
                break;
        }
    }

    private void SpawnCharacter(QuestControl ctrl)
    {
        string id = ctrl.characterId;
        if (id == "Player")
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Character/StoryPlayer");
            if (prefab == null)
            {
                Debug.LogError("未找到剧情主角预制体: Prefabs/StoryPlayer");
                return;
            }
            if (dynamicCharacters.ContainsKey("Player"))
                DestroyCharacter("Player");
            GameObject go = Instantiate(prefab, ctrl.position, Quaternion.identity);
            EnsureMover(go);
            dynamicCharacters["Player"] = go;
            return;
        }
        string path = $"Prefabs/Character/{id}";
        GameObject prefabNPC = Resources.Load<GameObject>(path);
        if (prefabNPC == null)
        {
            Debug.LogError($"未找到角色预制体: {path}");
            return;
        }
        if (dynamicCharacters.ContainsKey(id))
            DestroyCharacter(id);
        GameObject goNPC = Instantiate(prefabNPC, ctrl.position, Quaternion.identity);
        EnsureIdentifier(goNPC, id);
        EnsureMover(goNPC);
        dynamicCharacters[id] = goNPC;
    }

    private void EnsureIdentifier(GameObject go, string id)
    {
        var idComp = go.GetComponent<NPCIdentifier>();
        if (idComp == null) idComp = go.AddComponent<NPCIdentifier>();
        idComp.speakerId = id;
    }

    private void EnsureMover(GameObject go)
    {
        if (go.GetComponent<CharacterMover>() == null)
            go.AddComponent<CharacterMover>();
        if (go.GetComponent<Rigidbody2D>() == null)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }
    }

    private void DestroyCharacter(string id)
    {
        if (dynamicCharacters.TryGetValue(id, out GameObject go))
        {
            if (currentMoveCoroutines.TryGetValue(id, out var cor))
            {
                StopCoroutine(cor);
                currentMoveCoroutines.Remove(id);
            }
            Destroy(go);
            dynamicCharacters.Remove(id);
        }
    }

    private IEnumerator EnqueueMove(QuestControl ctrl)
    {
        string id = ctrl.characterId;
        if (currentMoveCoroutines.TryGetValue(id, out var existingMove))
            yield return existingMove;
        var moveCor = StartCoroutine(MoveCharacter(ctrl));
        currentMoveCoroutines[id] = moveCor;
        yield return moveCor;
        currentMoveCoroutines.Remove(id);
    }

    private IEnumerator MoveCharacter(QuestControl ctrl)
    {
        GameObject target = FindCharacter(ctrl.characterId);
        if (target == null)
        {
            Debug.LogWarning($"移动失败：找不到角色 {ctrl.characterId}");
            yield break;
        }
        var mover = target.GetComponent<CharacterMover>();
        if (mover == null) mover = target.AddComponent<CharacterMover>();
        yield return mover.MoveToPosition(ctrl.position, ctrl.moveSpeed);
    }

    private GameObject FindCharacter(string id)
    {
        if (dynamicCharacters.TryGetValue(id, out GameObject go))
            return go;
        var npcs = FindObjectsOfType<NPCIdentifier>();
        foreach (var npc in npcs)
            if (npc.speakerId == id)
                return npc.gameObject;
        if (id == "Player")
            return GameObject.FindGameObjectWithTag("Player");
        return null;
    }
}