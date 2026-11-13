using System.Collections.Generic;
using UnityEngine;

// 생성된 몬스터들의 리스트를 관리하는 매니저
public class MonsterManager : MonoBehaviour
{
    private static MonsterManager instance;
    public static MonsterManager Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindAnyObjectByType<MonsterManager>();

                if(instance == null)
                {
                    GameObject MonsterManager = new GameObject("MonsterManager");
                    instance = MonsterManager.AddComponent<MonsterManager>();
                }
            }
            return instance;
        }
    }

    private readonly HashSet<Monster> monsters = new HashSet<Monster>();
    public IReadOnlyCollection<Monster> Monsters => monsters;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            instance = null;
    }

    public void Register(Monster monster)
    {
        if(monster != null)
            monsters.Add(monster);
    }

    public void UnRegister(Monster monster)
    {
        monsters.Remove(monster);
    }
}
