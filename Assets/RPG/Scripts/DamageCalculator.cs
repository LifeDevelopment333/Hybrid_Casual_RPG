using UnityEngine;

public static class DamageCalculator
{
    /// <summary>
    /// 데미지 계산
    /// </summary>
    /// <param name="attacker"> 공격자 </param>
    /// <param name="defender"> 방어자 </param>
    /// <returns></returns>
    public static DamageResult Calculate(CharacterStatus attacker, CharacterStatus defender)
    {
        float attackDamage = attacker.AttackDamage;
        float defense = defender.Defense;

        if (attackDamage <= 0)
            return new DamageResult(0f, false);

        float damage = Mathf.Max(1, attackDamage - defense);

        float criticalChance = Mathf.Clamp01(attacker.CriticalChance);

        bool isCritical = Random.value < criticalChance;

        if(isCritical)
        {
            float criticalMultiplier = Mathf.Max(1f, attacker.CriticalDamage);

            damage *= criticalMultiplier;
        }

        return new DamageResult(damage, isCritical);
    }
}
