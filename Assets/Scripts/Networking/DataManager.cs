using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DataManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string baseUrl = "http://localhost:5000/api/Data";
    [SerializeField] private float trendUpdateInterval = 600f; // 10 minutes

    // --- Data Models ---
    [Serializable]
    public class BatteryHistoryPoint
    {
        // Must match JSON "timestamp"
        public string timestamp;
        public int whValue;
        public int maxWh;
    }

    [Serializable]
    public class PastWorldEntry
    {
        // Must match JSON "id"
        public int id;
        public int daysSurvived;
        public string resetReason;
        public string dateEnded;
    }

    [Serializable]
    public class PlayerLeaderboardEntry
    {
        // Must match JSON "username"
        public string username;
        public int loginStreak;
        public long totalContribution;
    }

    // Unity's JsonUtility wrapper helper
    [Serializable]
    private class ArrayWrapper<T> { public T[] items; }

    // --- Events ---
    public event Action<List<BatteryHistoryPoint>> OnTrendsReceived;

    private void Start()
    {
        StartCoroutine(TrendLoop());
    }

    private IEnumerator TrendLoop()
    {
        // Initial fetch then repeat
        while (true)
        {
            yield return StartCoroutine(GetTrendsRoutine());
            yield return new WaitForSeconds(trendUpdateInterval);
        }
    }

    // --- API Calls ---

    private IEnumerator GetTrendsRoutine()
    {
        string url = $"{baseUrl}/trends";
        using (UnityWebRequest uwr = CreateRequest(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                string rawJson = uwr.downloadHandler.text;

                // 1. Handle Empty Reply (Curl 52 / Server returned nothing)
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    Debug.LogWarning("Trend Fetch: Server returned empty string. Clearing graph.");
                    OnTrendsReceived?.Invoke(new List<BatteryHistoryPoint>());
                    yield break;
                }

                // 2. Handle standard JSON
                try
                {
                    string jsonWrapper = "{\"items\":" + rawJson + "}";
                    var wrapper = JsonUtility.FromJson<ArrayWrapper<BatteryHistoryPoint>>(jsonWrapper);

                    var list = (wrapper != null && wrapper.items != null)
                        ? new List<BatteryHistoryPoint>(wrapper.items)
                        : new List<BatteryHistoryPoint>();

                    OnTrendsReceived?.Invoke(list);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Trend JSON Parse Error: {e.Message}");
                    OnTrendsReceived?.Invoke(new List<BatteryHistoryPoint>());
                }
            }
            else
            {
                // Silent fail on connection errors to avoid spamming console
                // Debug.LogWarning($"Trend Fetch Error: {uwr.error}");
            }
        }
    }

    // --- LEADERBOARD REQUESTS ---

    public void RequestWorldLeaderboard(Action<List<PastWorldEntry>> callback)
    {
        StartCoroutine(GetWorldLeaderboardRoutine(callback));
    }

    private IEnumerator GetWorldLeaderboardRoutine(Action<List<PastWorldEntry>> callback)
    {
        string url = $"{baseUrl}/leaderboards/worlds";
        using (UnityWebRequest uwr = CreateRequest(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string json = uwr.downloadHandler.text;
                    Debug.Log($"[Data] Worlds JSON: {json}"); // LOG THE RAW JSON

                    string wrapped = "{\"items\":" + json + "}";
                    var wrapper = JsonUtility.FromJson<ArrayWrapper<PastWorldEntry>>(wrapped);

                    var list = (wrapper != null && wrapper.items != null)
                        ? new List<PastWorldEntry>(wrapper.items)
                        : new List<PastWorldEntry>();

                    callback?.Invoke(list);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Data] World Parse Error: {e.Message}");
                    callback?.Invoke(new List<PastWorldEntry>()); // Return empty so chain continues
                }
            }
            else
            {
                Debug.LogError($"[Data] World Request Failed: {uwr.error}");
                callback?.Invoke(new List<PastWorldEntry>()); // Return empty so chain continues
            }
        }
    }

    public void RequestPlayerLeaderboard(string type, Action<List<PlayerLeaderboardEntry>> callback)
    {
        StartCoroutine(GetPlayerLeaderboardRoutine(type, callback));
    }

    private IEnumerator GetPlayerLeaderboardRoutine(string type, Action<List<PlayerLeaderboardEntry>> callback)
    {
        string url = $"{baseUrl}/leaderboards/players?type={type}";
        using (UnityWebRequest uwr = CreateRequest(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string json = uwr.downloadHandler.text;
                    // Debug.Log($"[Data] Player ({type}) JSON: {json}");

                    string wrapped = "{\"items\":" + json + "}";
                    var wrapper = JsonUtility.FromJson<ArrayWrapper<PlayerLeaderboardEntry>>(wrapped);

                    var list = (wrapper != null && wrapper.items != null)
                        ? new List<PlayerLeaderboardEntry>(wrapper.items)
                        : new List<PlayerLeaderboardEntry>();

                    callback?.Invoke(list);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Data] Player ({type}) Parse Error: {e.Message}");
                    callback?.Invoke(new List<PlayerLeaderboardEntry>());
                }
            }
            else
            {
                Debug.LogError($"[Data] Player ({type}) Request Failed: {uwr.error}");
                callback?.Invoke(new List<PlayerLeaderboardEntry>());
            }
        }
    }

    private UnityWebRequest CreateRequest(string url)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(url);
        if (GameSession.Instance != null && !string.IsNullOrEmpty(GameSession.Instance.AuthToken))
        {
            uwr.SetRequestHeader("Authorization", "Bearer " + GameSession.Instance.AuthToken);
        }
        return uwr;
    }
}