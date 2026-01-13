using UnityEngine;
using UnityEngine.Events;

public class ItemSocket : BaseInteractable
{
    [Header("Socket Settings")]
    [SerializeField] private Transform pivotPoint;
    [SerializeField] private Item startingItem;
    [SerializeField] private bool isConsuming;

    [Header("Advanced")]
    public bool manualInteraction = false;
    public UnityEvent OnManualInteract; // The Dispenser listens to this

    [Header("Socket Events")]
    public UnityEvent<Item> OnItemPlaced;
    public UnityEvent OnItemRemoved;
    public UnityEvent OnItemConsumed;

    // Public getter for the Dispenser to check
    public Item HeldItem { get; private set; }
    public Transform Pivot => pivotPoint;

    protected override void Start()
    {
        base.Start();
        if (startingItem != null) ForcePlaceItem(startingItem);
    }

    public override void OnInteractStart()
    {
        base.OnInteractStart();

        // If Manual, just fire event and stop.
        if (manualInteraction)
        {
            OnManualInteract?.Invoke();
            return;
        }

        // Otherwise run standard logic
        HandleStandardInteraction();
    }

    private void HandleStandardInteraction()
    {
        PlayerHand hand = PlayerHand.Instance;
        if (!hand) return;

        bool socketHasItem = HeldItem != null;
        bool handHasItem = hand.HasItem();

        if (socketHasItem && !handHasItem)
        {
            // Take
            hand.EquipItem(RemoveItem());
        }
        else if (handHasItem)
        {
            if (isConsuming)
            {
                // Consume
                Item item = hand.RemoveItem();
                Destroy(item.gameObject);
                PlaySound(0);
                OnItemConsumed?.Invoke();
            }
            else if (socketHasItem)
            {
                // Swap
                Item returnItem = RemoveItem();
                Item handItem = hand.RemoveItem();
                hand.EquipItem(returnItem);
                PlaceItem(handItem);
            }
            else
            {
                // Place
                PlaceItem(hand.RemoveItem());
            }
        }
    }

    // --- Public Methods for Dispensers to use ---

    public void PlaceItem(Item item, bool silent = false)
    {
        if (item == null) return;
        HeldItem = item;

        // Snap to pivot
        item.transform.SetParent(pivotPoint);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        if (!silent)
        {
            PlaySound(0);
            OnItemPlaced?.Invoke(item);
        }
    }

    public void ForcePlaceItem(Item item) => PlaceItem(item, true);

    public Item RemoveItem(bool silent = false)
    {
        if (HeldItem == null) return null;

        Item item = HeldItem;
        HeldItem = null;

        if (!silent)
        {
            PlaySound(1);
            OnItemRemoved?.Invoke();
        }
        return item;
    }
}