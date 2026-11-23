using UnityEngine;

[CreateAssetMenu(
    fileName = "Item",
    menuName = "RPG/Item Data"
    )]
public class ItemData : ScriptableObject
{
    public string id;
    public string itemName;

    public RewardType type;
    public ItemRarity rarity;

    public Sprite icon;
    public GameObject prefab;
}
