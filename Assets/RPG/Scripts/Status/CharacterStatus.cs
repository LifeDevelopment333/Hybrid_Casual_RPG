using System.Collections.Generic;
using UnityEngine;

public class CharacterStatus : MonoBehaviour
{
    private List<StatModifier> modifiers = new List<StatModifier>();
    [SerializeField] private CharacterStatusData baseData;

    private readonly Dictionary<StatTypes, float> cachedStats = new Dictionary<StatTypes, float>();

    public float MaxHp => GetStat(StatTypes.MaxHP, baseData.maxHp);
    public float MaxMp => GetStat(StatTypes.MaxMP, baseData.maxMp);

    public float AttackDamage => GetStat(StatTypes.AttackDamage, baseData.attackDamage);
    public float Defense => GetStat(StatTypes.Defense, baseData.defense);
    public float MoveSpeed => GetStat(StatTypes.MoveSpeed, baseData.moveSpeed);
    public float AttackSpeed => GetStat(StatTypes.AttackSpeed, baseData.attackSpeed);

    public float HpRegen => GetStat(StatTypes.HpRegen, baseData.hpRegen);
    public float MpRegen => GetStat(StatTypes.MPRegen, baseData.mpRegen);

    public float Accuracy => GetStat(StatTypes.Accuracy, baseData.accuracy);
    public float CriticalChance => GetStat(StatTypes.CriticalChance, baseData.criticalChance);
    public float CriticalDamage => GetStat(StatTypes.CriticalDamage, baseData.criticalDamage);

    public float AttackRange => GetStat(StatTypes.AttackRange, baseData.attackRange);
    public float AttackCooldown => GetStat(StatTypes.AttackCooldown, baseData.attackCooldown);

    private void OnEnable()
    {
        InvalidateStats();
    }

    private void OnValidate()
    {
        InvalidateStats();
    }

    private float GetStat(StatTypes statType, float baseValue)
    {
        if (cachedStats.TryGetValue(statType, out float value))
            return value;

        value = CalculateStat(statType, baseValue);
        cachedStats[statType] = value;

        return value;
    }

    // Call after changing level growth, base data, or an existing modifier's value.
    public void InvalidateStats()
    {
        cachedStats.Clear();
    }

    /// <summary>
    /// 스탯 보정치 계산
    /// </summary>
    /// <param name="statType"> 스탯 종류 </param>
    /// <param name="value"> 합/곱연산 값 </param>
    /// <returns></returns>
    private float CalculateStat(StatTypes statType, float value)
    {
        float flat = 0f;
        float percent = 0f;

        foreach (StatModifier modifier in modifiers)
        {
            if (modifier == null || modifier.statType != statType) continue;

            switch (modifier.modifierType)
            {
                case StatModifierType.Flat:
                    flat += modifier.value;
                    break;
                case StatModifierType.Precent:
                    percent += modifier.value;
                    break;
            }
        }

        return (value + flat) * (1f + percent);
    }

    /// <summary>
    /// 스탯 보정치 추가
    /// </summary>
    /// <param name="modifier"> 보정 능력치 </param>
    public void AddModifier(StatModifier modifier)
    {
        if(modifier == null || modifiers.Contains(modifier)) return;

        modifiers.Add(modifier);
        InvalidateStats();
    }

    /// <summary>
    /// 스탯 보정치 제거
    /// </summary>
    /// <param name="modifier"> 보정 능력치 </param>
    public void RemoveModifier(StatModifier modifier)
    {
        if (modifiers.Remove(modifier))
            InvalidateStats();
    }
}