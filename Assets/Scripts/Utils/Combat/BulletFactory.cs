using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public static class BulletFactory
{
    /// <summary>
    /// 根据法术包生成子弹，并应用修饰类
    /// </summary>
    public static GameObject CreateBullet(SpellPackage spellPackage, Vector2 position, Vector2 direction, GameObject owner)
    {
        if (spellPackage.projectile == null || spellPackage.projectile.prefab == null)
        {
            Debug.LogError("投射类定义或预制体缺失");
            return null;
        }

        // 基础属性
        float speed = spellPackage.projectile.speed;
        int damage = (int)(PlayerDataManager.Instance.CurrentPlayerData.GetTotalStatsAtLevel(PlayerDataManager.Instance.CurrentPlayerData.Level).Attack * (1 + Player.Instance.gameObject.GetComponent<BuffController>().GetAttackBonusPercent()) * spellPackage.projectile.damage); // 基础伤害，后续可叠加角色攻击力
        int num = 1;
        int fieldAngle = 0;
        float recoilForce = spellPackage.projectile.recoilForce;

        // 应用修饰类（修改子弹属性）
        foreach (var modifier in spellPackage.modifiers)
        {
            Debug.Log(damage);
            ApplyModifier(modifier, ref speed, ref damage, ref num, ref fieldAngle, ref recoilForce);
        }

        // 应用后坐力：对玩家施加与子弹速度方向相反的作用力
        if (owner != null && owner.CompareTag("Player"))
        {
            Rigidbody2D playerRb = owner.GetComponent<Rigidbody2D>();
            if (playerRb != null && recoilForce != 0f)
            {
                Vector2 recoil = -direction.normalized * 0.1f * recoilForce;
                playerRb.AddForce(recoil, ForceMode2D.Impulse);
            }
        }


        for (int i = 0; i < Mathf.Min(num, 100); i++)
        {
            // 计算最终方向：若 fieldAngle > 0，则在 [ -fieldAngle/2 , fieldAngle/2 ] 范围内随机旋转
            Vector2 finalDirection = direction;
            if (fieldAngle > 0)
            {
                float randomAngle = Random.Range(-fieldAngle * 0.5f, fieldAngle * 0.5f);
                finalDirection = Quaternion.Euler(0, 0, randomAngle) * direction;
            }

            // 从对象池获取子弹对象（传入预制体）
            GameObject bulletObj = BulletPool.Instance.GetBullet(spellPackage.projectile.prefab);
            Debug.Log(bulletObj);
            bulletObj.transform.position = position;
            bulletObj.transform.rotation = Quaternion.identity;

            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet == null)
            {
                Debug.LogError("子弹预制体没有 Bullet 组件");
                BulletPool.Instance.ReturnBullet(bulletObj, spellPackage.projectile.prefab); // 归还
                return null;
            }

            // 初始化子弹（传递修正类列表和源预制体）
            bullet.Initialize(spellPackage.projectile.id, finalDirection, owner, speed * Random.Range(0.5f, 2.0f), damage / num, position,
                             spellPackage.correctors, spellPackage.projectile.prefab, spellPackage.projectile.lifetime);
        }
        return null;
    }

    private static void ApplyModifier(SpellModuleSO modifier, ref float speed, ref int damage, ref int num, ref int fieldAngle, ref float recoilForce)
    {
        // 根据修饰类参数修改属性
        if (modifier.speedMultiplier != 0) speed *= modifier.speedMultiplier;
        if (modifier.damageMultiplier != 0) damage = (int)(damage * modifier.damageMultiplier);
        if (modifier.splitCount != 0) num *= modifier.splitCount;
        if (modifier.fieldAngle != 0) fieldAngle = modifier.fieldAngle;
        if (recoilForce != 0) recoilForce *= modifier.recoilForce;
        // 这里可以添加更多修饰效果，例如改变子弹颜色、添加粒子等
        // 注意：修饰类不立即执行特殊行为（如分裂），分裂等行为属于修正类，在子弹飞行中执行
    }
}