using System.Collections.Generic;

/// <summary>
/// 法术包：一个投射类 + 它前面的修饰类 + 它后面的修正类
/// </summary>
public class SpellPackage
{
    public BulletDefineSO projectile;              // 投射类定义的子弹
    public List<SpellModuleSO> modifiers;          // 修饰类列表（初始化时应用）
    public List<SpellModuleSO> correctors;         // 修正类列表（按延迟顺序执行）

    public SpellPackage(BulletDefineSO proj)
    {
        projectile = proj;
        modifiers = new List<SpellModuleSO>();
        correctors = new List<SpellModuleSO>();
    }
}