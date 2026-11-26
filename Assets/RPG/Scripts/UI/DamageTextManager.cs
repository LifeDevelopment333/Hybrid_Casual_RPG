using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class DamageTextManager : MonoBehaviour
{
    private static DamageTextManager instance;
    public static DamageTextManager Instance => instance;

    [SerializeField] private FloatingDamageText prefab;
    private RectTransform canvasRect;

    [SerializeField] private int poolMaxSize = 20;
    [SerializeField] private int maxVisiableSize = 10;

    private ObjectPool<FloatingDamageText> pool;
    private List<FloatingDamageText> active;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        canvasRect = GetComponent<RectTransform>();

        active = new List<FloatingDamageText>(maxVisiableSize);

        pool = new ObjectPool<FloatingDamageText>(
            createFunc: CreateDamageText,
            actionOnGet: null,
            actionOnRelease: item => item.Hide(),
            actionOnDestroy: item => Destroy(item.gameObject),
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: poolMaxSize
            );
    }

    public FloatingDamageText CreateDamageText()
    {
        FloatingDamageText item = Instantiate(prefab, transform);
        item.Initialize();

        return item;
    }

    public void Show(DamageResult damageResult, Vector3 position)
    {
        if (active.Count >= maxVisiableSize) return;

        FloatingDamageText item = pool.Get();

        item.Show(damageResult, canvasRect, position);

        active.Add(item);
    }

    private void LateUpdate()
    {
        if (active.Count <= 0) return;

        for(int i = active.Count - 1; i >= 0; i--)
        {
            bool isPlaying = active[i].Tick();

            if (isPlaying) continue;

            pool.Release(active[i]);
            active.RemoveAt(i);
        }
    }
}
