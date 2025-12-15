using System;

public class TaskGenerator
{
    private uint _state;

    public TaskGenerator(int seed)
    {
        _state = (uint)seed;
    }

    // A lightweight "Next" function (Linear Congruential Generator)
    // Formula: state = state * 1664525 + 1013904223
    // This is the algorithm used by the QuickC "rand" function.
    private uint NextUint()
    {
        _state = _state * 1664525 + 1013904223;
        return _state;
    }

    /// <summary>
    /// Generates the 4 boolean values for the next task.
    /// </summary>
    public TaskData GetNextTask()
    {
        uint rnd = NextUint();

        // We only need the last 4 bits (0-15)
        // Bitwise operations are faster and cleaner than modulo for powers of 2
        bool val1 = (rnd & 1) != 0;
        bool val2 = (rnd & 2) != 0;
        bool val3 = (rnd & 4) != 0;
        bool val4 = (rnd & 8) != 0;

        return new TaskData(val1, val2, val3, val4);
    }
}

// Simple container for your 4 booleans
[Serializable]
public struct TaskData
{
    public bool IsTempHigh;
    public bool IsFlowHigh;
    public bool IsRPMHigh;
    public bool IsVoltageHigh;

    public TaskData(bool t, bool p, bool v, bool r)
    {
        IsTempHigh = t;
        IsFlowHigh = p;
        IsVoltageHigh = v;
        IsRPMHigh = r;
    }

    public override string ToString()
    {
        return $"{(IsTempHigh ? 1 : 0)}{(IsFlowHigh ? 1 : 0)}{(IsVoltageHigh ? 1 : 0)}{(IsRPMHigh ? 1 : 0)}";
    }
}