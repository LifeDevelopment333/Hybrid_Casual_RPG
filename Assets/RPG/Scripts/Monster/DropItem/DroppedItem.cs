using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    private static readonly List<DroppedItem> active = new List<DroppedItem>();
    public static IReadOnlyList<DroppedItem> Active => active;

    private ItemData itemData;
    private int amount;

    public ItemData Data => itemData;
    public int Amount => amount;

    public void Initialize(ItemData data, int count)
    {
        itemData = data;
        amount = count;
    }

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }
}
