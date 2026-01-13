using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/CalibrationDatabase")]
public class CalibrationDatabase : ScriptableObject
{
    [Serializable]
    public struct CalibrationEntry
    {
        [Header("Inputs (The 4 Nixies)")]
        // Helpful name in inspector, e.g. "0101 (Temp/Volt)"
        public string debugName;

        [Header("Step 1: The Switch Code")]
        public int correctLetter; // 0=A, 1=B, 2=C...
        public int correctNumber; // 0=1, 1=2, 2=3...

        [Header("Step 2: The Components")]
        public Item.ItemType[] requiredItems; // Should always be size 3
    }

    // Array of 16 entries (indices 0 to 15)
    public CalibrationEntry[] entries;

    public CalibrationEntry GetSolution(TaskData task)
    {
        // Convert the 4 bools into a 0-15 integer index
        // Bitwise: Temp(8) Flow(4) Volt(2) RPM(1)
        int index = 0;
        if (task.IsTempHigh) index |= 8;
        if (task.IsFlowHigh) index |= 4;
        if (task.IsVoltageHigh) index |= 2;
        if (task.IsRPMHigh) index |= 1;

        if (index >= entries.Length) return entries[0];
        return entries[index];
    }
}