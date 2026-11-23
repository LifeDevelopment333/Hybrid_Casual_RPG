using System.Collections.Generic;
using UnityEngine;

public class PlayerStatPoint : MonoBehaviour
{
    private CharacterStatus status;
    private PlayerLevel playerLevel;

    // 투자된 스텟포인트
    private readonly Dictionary<StatTypes, int> investPoints = new Dictionary<StatTypes, int>();

    private readonly Dictionary<StatTypes, StatModifier> appliedModifiers = new Dictionary<StatTypes, StatModifier>();

    private int usedPoints;

    public int AvailablePoints => playerLevel.Level - 1 - usedPoints;

    private void Awake()
    {
        status = GetComponent<CharacterStatus>();
        playerLevel = GetComponent<PlayerLevel>();
    }

    public int GetInvestedPoints(StatTypes statType)
    {
        investPoints.TryGetValue(statType, out int points);
        return points;
    }

    public bool TryInvest(StatTypes statType)
    {
        if (AvailablePoints <= 0) return false;

        float bonusPerPoint = 0;

        switch (statType)
        {
            case StatTypes.MaxHP:
                bonusPerPoint = 100;
                break;
            case StatTypes.MaxMP:
                bonusPerPoint = 100;
                break;
            case StatTypes.AttackDamage:
                bonusPerPoint = 10;
                break;
            case StatTypes.Defense:
                bonusPerPoint = 5;
                break;
            case StatTypes.MoveSpeed:
                bonusPerPoint = 0.01f;
                break;
            case StatTypes.AttackSpeed:
                bonusPerPoint = 0.01f;
                break;
            case StatTypes.HpRegen:
                bonusPerPoint = 1;
                break;
            case StatTypes.MPRegen:
                bonusPerPoint = 1;
                break;
            case StatTypes.Accuracy:
                bonusPerPoint = 0.5f;
                break;
            case StatTypes.CriticalChance:
                bonusPerPoint = 0.01f;
                break;
            case StatTypes.CriticalDamage:
                bonusPerPoint = 1;
                break;
            case StatTypes.AttackRange:
                break;
            case StatTypes.AttackCooldown:
                break;
        }

        if(bonusPerPoint <= 0) return false;

        int points = GetInvestedPoints(statType) + 1;
        investPoints[statType] = points;
        usedPoints++;

        float bonus = points * bonusPerPoint;

        if(appliedModifiers.TryGetValue(statType, out StatModifier modifier))
        {
            modifier.value = bonus;
            status.InvalidateStats();
        }
        else
        {
            modifier = new StatModifier
            {
                statType = statType,
                modifierType = StatModifierType.Flat,
                value = bonus
            };

            appliedModifiers.Add(statType, modifier);
            status.AddModifier(modifier);
        }
        
        return true;
    }

    /// <summary>
    /// 스탯 초기화
    /// </summary>
    public void ResetAllocation()
    {
        foreach (StatModifier modifier in appliedModifiers.Values)
        {
            status.RemoveModifier(modifier);
        }

        appliedModifiers.Clear();
        investPoints.Clear();
        usedPoints = 0;
    }
}
