using System;

[Serializable]
public class MiniGameStartRequest
{
    public string Intention; // "CHARGE" or "DISCHARGE"
}

[Serializable]
public class MiniGameStartResponse
{
    public string miniGameId;
    public int seed;
}

[Serializable]
public class MiniGameSubmitRequest
{
    public string MiniGameId;
    public int Seed;
    public int TasksCompleted;
    public int BonusTasks;
}

[Serializable]
public class MiniGameSubmitResponse
{
    public long newBatteryTotal;
    public long energyApplied;
    public double efficiency;
}