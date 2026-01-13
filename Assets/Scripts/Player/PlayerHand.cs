using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public static PlayerHand Instance { get; private set; }

    [SerializeField] private Transform handPivot; // Where the item sits visually

    public Item CurrentItem { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;
    }

    public void EquipItem(Item item)
    {
        if (CurrentItem != null) DropItem(); // Safety check

        CurrentItem = item;
        item.transform.SetParent(handPivot);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    public Item RemoveItem()
    {
        Item item = CurrentItem;
        CurrentItem = null;
        return item;
    }

    // Helper: Drop to ground (optional)
    public void DropItem()
    {
        if (CurrentItem == null) return;

        CurrentItem.transform.SetParent(null);
        CurrentItem = null;
    }

    public bool HasItem() => CurrentItem != null;
}