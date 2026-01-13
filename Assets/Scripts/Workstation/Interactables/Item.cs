using UnityEngine;

public class Item : MonoBehaviour
{
    public enum ItemType { VacuumTube, Capacitor, Inductor }

    [Header("Item Settings")]
    public ItemType itemType;
}