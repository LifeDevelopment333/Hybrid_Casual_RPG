using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Equipment",
    menuName = "RPG/Equipment Data"
    )]
public class EquipmentData : ItemData
{
    public EquipmentSlot slot;
    public List<StatModifier> modifiers = new List<StatModifier>();
}
