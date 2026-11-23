using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private readonly Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();
    [SerializeField] private float pickupDistance = 1f;

    private float pickupTimer;

    public bool AddItem(ItemData item, int itemCount)
    {
        if (item == null || itemCount <= 0) return false;

        items.TryGetValue(item, out int currentCount);
        items[item] = currentCount + itemCount;

        return true;
    }

    private void Update()
    {
        pickupTimer += Time.deltaTime;

        if (pickupTimer < 0.1f) return;

        pickupTimer = 0;
        CollectNearbyItems();
    }

    private void CollectNearbyItems()
    {
        var droppedItems = DroppedItem.Active;

        for (int i = droppedItems.Count - 1; i >= 0; i--)
        {
            DroppedItem item = droppedItems[i];
            if (item == null) continue;

            Vector3 offset = Vector3.ProjectOnPlane(item.transform.position - transform.position, Vector3.up);

            if (offset.sqrMagnitude > pickupDistance * pickupDistance) continue;
            if (AddItem(item.Data, item.Amount) == false) continue;

            Debug.Log($"{item.Data} {item.Amount} 개 획득");

            // 나중에 최적화 필요
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }
    }
}
