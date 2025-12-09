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

    /// <summary>
    /// Clears session data (use this for Logout)
    /// </summary>
    public void ClearSession()
    {
        AuthToken = null;
        Username = null;
        PlayerId = 0;
    }
}