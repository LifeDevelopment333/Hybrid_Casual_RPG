using UnityEngine;

[CreateAssetMenu(
    fileName = "_Status_Data",
    menuName = "RPG/Character Status Data"
    )]
public class CharacterStatusData : ScriptableObject
{
    public float maxHp = 100f;                              // HP
    public float maxMp = 50f;                               // MP

    public float attackDamage = 10f;                        // 공격력
    public float defense = 0f;                              // 방어력
    public float moveSpeed = 4f;                            // 이동속도
    public float attackSpeed = 1f;                          // 공격속도

    // 초당 회복량
    public float hpRegen = 0f;                              // HP 회복량
    public float mpRegen = 0f;                              // MP 회복량

    public float accuracy = 1f;                             // 명중률
    [Range(0,1)] public float criticalChance = 0f;          // 치명타 확률
    public float criticalDamage = 1.5f;                     // 치명타 피해

    public float attackRange = 1.5f;                        // 공격사거리
    public float attackCooldown = 1f;                       // 공격쿨타임
}