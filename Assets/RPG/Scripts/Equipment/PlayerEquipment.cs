using System.Collections.Generic;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    private CharacterStatus status;

    // 현재 장착된 장비
    private readonly Dictionary<EquipmentSlot, EquipmentData> equipped = new Dictionary<EquipmentSlot, EquipmentData>();

    private void Awake()
    {
        status = GetComponent<CharacterStatus>();
    }

    public EquipmentData GetEquiment(EquipmentSlot slot)
    {
        equipped.TryGetValue(slot, out EquipmentData equippedData);
        return equippedData;
    }

    /// <summary>
    /// 장비 장착
    /// </summary>
    /// <param name="equipment"> 장착될 장비 </param>
    /// <returns> 성공여부 </returns>
    public bool Equip(EquipmentData equipment)
    {
        if (equipment == null) return false;

        EquipmentSlot slot = equipment.slot;

        // 같은 장비 중복체크
        if (GetEquiment(slot) == equipment) return false;

        Unequip(slot);

        equipped.Add(slot, equipment);

        foreach(StatModifier modifier in equipment.modifiers)
        {
            status.AddModifier(modifier);
        }

        return true;
    }

    /// <summary>
    /// 장비 해제
    /// </summary>
    /// <param name="slot"> 부위 선택 </param>
    /// <returns> 해제된 장비 </returns>
    public EquipmentData Unequip(EquipmentSlot slot)
    {
        if (equipped.TryGetValue(slot, out EquipmentData equippedData) == false)
            return null;

        foreach(StatModifier modifier in equippedData.modifiers)
        {
            status.RemoveModifier(modifier);
        }

        equipped.Remove(slot);

        return equippedData;
    }
}
