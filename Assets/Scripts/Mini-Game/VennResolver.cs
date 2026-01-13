using UnityEngine;
using System;

[CreateAssetMenu(fileName = "VennResolver", menuName = "Game/VennResolver")]
public class VennResolver : ScriptableObject
{
    [Serializable]
    public struct VennEntry
    {
        public string debugName; // e.g. "0000" or "TFFF"
        public int letterIndex;  // 0=A, 1=B, 2=C...
        public int numberIndex;  // 0=1, 1=2...
    }

    // We need exactly 16 entries (0 to 15) covering all binary combinations
    public VennEntry[] entries = new VennEntry[16];

    public VennEntry GetCoordinate(TaskData task)
    {
        // Convert bools to index 0-15 (8*T + 4*F + 2*V + 1*R)
        int index = 0;
        if (task.IsTempHigh) index |= 8;
        if (task.IsFlowHigh) index |= 4;
        if (task.IsVoltageHigh) index |= 2;
        if (task.IsRPMHigh) index |= 1;

        if (index >= entries.Length) return entries[0];
        return entries[index];
    }
}