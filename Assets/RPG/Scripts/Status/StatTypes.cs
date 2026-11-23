using System;
using UnityEngine;

public enum StatTypes
{
    MaxHP,
    MaxMP,

    AttackDamage,
    Defense,
    MoveSpeed,
    AttackSpeed,

    HpRegen,
    MPRegen,

    Accuracy,
    CriticalChance,
    CriticalDamage,

    AttackRange,
    AttackCooldown
}

public enum StatModifierType
{
    Flat,
    Precent
}

[Serializable]
public class StatModifier
{
    public StatTypes statType;
    public StatModifierType modifierType;
    public float value;
}
