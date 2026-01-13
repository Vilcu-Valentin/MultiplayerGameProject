using System.Collections;
using UnityEngine;

public abstract class DispenserBase : MonoBehaviour
{
    [Header("Dispenser Visuals")]
    [SerializeField] protected ItemSocket targetSocket;
    [SerializeField] protected Transform internalSpawnPoint; // Inside the machine
    [SerializeField] protected float conveySpeed = 2f;

    // Helper to animate an item from Inside -> Socket
    protected IEnumerator AnimateToSocket(Item item)
    {
        // 1. Setup initial position (Inside machine)
        item.transform.SetParent(targetSocket.transform); // Temp parent
        item.transform.position = internalSpawnPoint.position;
        item.transform.rotation = internalSpawnPoint.rotation;

        // 2. Move to Socket Pivot
        Vector3 startPos = internalSpawnPoint.position;
        Vector3 endPos = targetSocket.Pivot.position;
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * conveySpeed;
            item.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // 3. Finalize
        targetSocket.PlaceItem(item); // Actually link it to the socket logic
    }

    // Helper to animate Socket -> Inside (Stack Push)
    protected IEnumerator AnimateFromSocket(Item item, System.Action onComplete)
    {
        // 1. Unlink from socket logic immediately
        targetSocket.RemoveItem(true);

        Vector3 startPos = item.transform.position;
        Vector3 endPos = internalSpawnPoint.position;
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime * conveySpeed;
            item.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        onComplete?.Invoke();
    }
}