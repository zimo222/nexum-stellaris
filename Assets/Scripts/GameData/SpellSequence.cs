using System.Collections.Generic;

/// <summary>
/// 法术序列：由多个步骤组成，每个步骤可以是 SpellPackage 或 MultiCastNode
/// </summary>
public class SpellSequence
{
    public List<object> steps;   // 元素类型：SpellPackage 或 MultiCastNode

    public SpellSequence()
    {
        steps = new List<object>();
    }
}

/// <summary>
/// 多重释放节点：表示接下来连续执行 N 个 SpellPackage（无间隔）
/// </summary>
public class MultiCastNode
{
    public int count;   // 同时释放的数量

    public MultiCastNode(int count)
    {
        this.count = count;
    }
}