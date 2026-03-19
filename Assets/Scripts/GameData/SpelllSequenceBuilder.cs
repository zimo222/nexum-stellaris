using System.Collections.Generic;
using UnityEngine;

public static class SpellSequenceBuilder
{
    /// <summary>
    /// 从模块ID列表构建法术序列
    /// </summary>
    public static SpellSequence BuildSequence(List<string> moduleIds)
    {
        var sequence = new SpellSequence();
        if (moduleIds == null || moduleIds.Count == 0)
            return sequence;

        // 第一步：将ID转换为模块对象
        List<SpellModuleSO> modules = new List<SpellModuleSO>();
        foreach (string id in moduleIds)
        {
            if (GameDataManager.Instance.SpellModuleDict.TryGetValue(id, out SpellModuleSO module))
                modules.Add(module);
            else
                Debug.LogWarning($"未找到模块ID: {id}");
        }

        // 第二步：解析为步骤
        List<SpellModuleSO> pendingModifiers = new List<SpellModuleSO>(); // 暂存遇到的修饰类
        SpellPackage currentPackage = null;

        for (int i = 0; i < modules.Count; i++)
        {
            SpellModuleSO module = modules[i];

            switch (module.moduleType)
            {
                case SpellModuleType.Modifier:
                    // 修饰类：暂存起来，等待后面的投射类
                    pendingModifiers.Add(module);
                    break;

                case SpellModuleType.Projectile:
                    // 投射类：创建包，将暂存的修饰类加入，并清空暂存列表
                    currentPackage = new SpellPackage(module.bulletDefine);
                    currentPackage.modifiers.AddRange(pendingModifiers);
                    pendingModifiers.Clear();

                    // 继续向后查找修正类（属于当前包）
                    int j = i + 1;
                    while (j < modules.Count && (modules[j].moduleType == SpellModuleType.Corrector))
                    {
                        // 注意：修正类只影响前面的投射类，修饰类不应该在这里（但如果是修饰类，它应该影响后面的投射类，所以跳过）
                        if (modules[j].moduleType == SpellModuleType.Corrector)
                            currentPackage.correctors.Add(modules[j]);
                        j++;
                    }
                    // 将当前包加入序列步骤
                    sequence.steps.Add(currentPackage);
                    // 跳过已处理的修正类（i 移动到 j-1，因为循环结束会 i++）
                    i = j - 1;
                    break;

                case SpellModuleType.Corrector:
                    // 修正类如果没有前面的投射类，则忽略（打印警告）
                    Debug.LogWarning($"修正类 {module.id} 前没有投射类，已忽略");
                    break;

                case SpellModuleType.MultiCast:
                    // 多重释放：添加节点
                    sequence.steps.Add(new MultiCastNode(module.multicastCount));
                    break;
            }
        }

        // 如果还有未使用的修饰类，忽略
        if (pendingModifiers.Count > 0)
        {
            Debug.LogWarning($"存在未使用的修饰类: {string.Join(", ", pendingModifiers)}");
        }

        return sequence;
    }
}