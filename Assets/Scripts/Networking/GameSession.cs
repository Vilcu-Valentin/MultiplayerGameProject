using UnityEngine;

/// <summary>
/// Singleton class to hold persistent data across scenes.
/// This object will not be destroyed when loading new scenes.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    [Header("Session Data")]
    public string AuthToken;
    public string Username;
    public int PlayerId; // Optional: if you want to store ID later
    public long TotalContribution;
    public int LoginStreak;

    public GameOverReport PendingReport;

    private void Awake()
    {
        // Singleton pattern: Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;
            // This makes the GameObject persist when the scene changes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If a duplicate exists (e.g., returning to the menu), destroy it to prevent conflicts
            Destroy(gameObject);
        }
    }

    public void AddContribution(long amount)
    {
        TotalContribution += amount;
    }

    /// <summary>
    /// Clears session data (use this for Logout)
    /// </summary>
    public void ClearSession()
    {
        AuthToken = null;
        Username = null;
        PlayerId = 0;
        PendingReport = null;
    }
}

[System.Serializable]
public class GameOverReport
{
    public string type;
    public string reason;
    public int daysSurvived;
    public string dateEnded; // Unity can't parse DateTime automatically from JSON, string is safer
}