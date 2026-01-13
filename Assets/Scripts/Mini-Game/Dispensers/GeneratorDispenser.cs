using System.Collections;
using System.Linq;
using UnityEngine;

public class GeneratorDispenser : DispenserBase
{
    [Header("Generator Settings")]
    [SerializeField] private GameObject[] itemPrefab;
    [SerializeField] private float dispenseDelay = 0.5f; // Time before new item starts appearing

    private bool _isDispensing = false;

    private void Start()
    {
        // Listen to the socket
        // When an item is taken (or swapped out), check if we need to refill
        targetSocket.OnItemRemoved.AddListener(OnSocketItemRemoved);

        // Initial Check: If we start empty, dispense immediately
        if (targetSocket.HeldItem == null)
        {
            AttemptDispense();
        }
    }

    private void OnSocketItemRemoved()
    {
        // We wait a tiny bit. Why?
        // Because if the player is SWAPPING, the socket becomes empty for 1 frame
        // and then immediately full again. We don't want to spawn an item in that split second.
        StartCoroutine(WaitAndCheck());
    }

    private IEnumerator WaitAndCheck()
    {
        // Wait for end of frame (handles the Swap logic where PlaceItem happens immediately after Remove)
        yield return new WaitForEndOfFrame();

        // If after the swap the socket is STILL empty, then we dispense.
        if (targetSocket.HeldItem == null)
        {
            // Optional: Add a small mechanical delay (e.g. machine processing time)
            if (dispenseDelay > 0) yield return new WaitForSeconds(dispenseDelay);

            AttemptDispense();
        }
    }

    public void AttemptDispense()
    {
        // Safety checks
        if (_isDispensing) return;
        if (targetSocket.HeldItem != null) return;
        if (itemPrefab.Length <= 0) return;

        StartCoroutine(DispenseRoutine());
    }

    public GameObject SelectItem()
    {
        return itemPrefab[Random.Range(0, itemPrefab.Length)];
    }

    private IEnumerator DispenseRoutine()
    {
        _isDispensing = true;

        // Create the item internally
        GameObject newObj = Instantiate(SelectItem());
        Item newItem = newObj.GetComponent<Item>();

        // Run the conveyor belt animation (from Base Class)
        // This moves it from InternalPoint -> SocketPivot
        yield return StartCoroutine(AnimateToSocket(newItem));

        _isDispensing = false;
    }
}