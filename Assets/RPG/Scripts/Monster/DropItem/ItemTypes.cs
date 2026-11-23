using System;
using UnityEngine;

public enum RewardType
{
    Item,
    Currency,
    Material,
}

public enum ItemRarity
{
    Common,
    Rare,
    Epic
}

[Serializable]
public class RewardEntry
{
    public ItemData itemData;         // 아이템 보상일 때 사용할 원본 아이템 데이터

    [Min(1)] public int minAmount = 1;           // 재화, 재료, 경험치 수량
    [Min(1)] public int maxAmount = 1;

    [Range(0, 1)] public float weight;                // 랜덤 선택 가중치
    // 가중치 방식은 테이블을 수정하기 쉽고, 항목이 추가되어도 전체 퍼센트를 매번 다시 계산하지 않아도 된다는 장점이 있습니다.

    public int RollAmount()
    {
        return UnityEngine.Random.Range(minAmount, maxAmount + 1);
    }
}