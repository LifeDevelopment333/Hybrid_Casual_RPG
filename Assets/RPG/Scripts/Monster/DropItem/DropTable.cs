using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DropTable",
    menuName = "RPG/DropTable"
    )]
public class DropTable : ScriptableObject
{
    public List<RewardEntry> rewards = new List<RewardEntry>();

    public RewardEntry Roll()
    {
        if (rewards == null) return null;

        float totalWeight = 0f;
        RewardEntry lastValid = null;

        foreach (RewardEntry entry in rewards)
        {
            if (entry == null || entry.itemData == null || entry.weight <= 0f)
                continue;

            totalWeight += entry.weight;
            lastValid = entry;
        }

        if (lastValid == null) return null;

        float roll = Random.value * totalWeight;

        foreach (RewardEntry entry in rewards)
        {
            if (entry == null || entry.itemData == null || entry.weight <= 0f)
                continue;

            if (roll < entry.weight)
                return entry;

            roll -= entry.weight;
        }

        // 난수의 상한이나 부동소수점 오차로 끝까지 도달한 경우
        return lastValid;
    }
}
