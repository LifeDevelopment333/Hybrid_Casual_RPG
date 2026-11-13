using UnityEngine;

public class MonsterSpawn : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Monster monster;
    [SerializeField] private Transform player;

    [Header("옵션")]
    // 생성될 최소/최대 거리
    [SerializeField] private float minSapwnDistance = 4f;
    [SerializeField] private float maxSpawnDistance = 8f;

    // 스폰 주기
    [SerializeField] private float spawnInterval = 2f;
    private float spawnTimer;

    private Vector3 GetSpawnPosition()
    {
        // 방향
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

        // 거리
        // 원형 영역의 면적에 고르게 분포하도록 하기 위해 제곱근을 사용
        float distance = Mathf.Sqrt(Random.Range(minSapwnDistance * minSapwnDistance, maxSpawnDistance * maxSpawnDistance));

        return player.position + direction * distance;
    }

    private void SpawnMonster()
    {
        if (monster == null || player == null) return;

        Vector3 spawnPosition = GetSpawnPosition();

        Monster spawnMonster = Instantiate(monster, spawnPosition, Quaternion.identity);
        MonsterManager.Instance.Register(spawnMonster);
        spawnMonster.Initialize(player);
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer > spawnInterval)
        {
            spawnTimer = 0f;
            SpawnMonster();
        }
    }
}
