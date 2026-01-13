using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardDisplay : MonoBehaviour
{
    public enum BoardMode { PastWorlds, Contribution, Streak }

    [Header("Dependencies")]
    [SerializeField] private DataManager dataManager;

    [Header("Displays")]
    [SerializeField] private SplitFlapDisplay[] rows;
    [SerializeField] private TMP_Text headerText;

    private BoardMode _currentMode = BoardMode.PastWorlds;
    private int _scrollIndex = 0;
    private List<string> _formattedLines = new List<string>();

    // Caches
    private List<DataManager.PastWorldEntry> _cacheWorlds;
    private List<DataManager.PlayerLeaderboardEntry> _cacheContrib;
    private List<DataManager.PlayerLeaderboardEntry> _cacheStreak;

    private void Start()
    {
        // Check references
        if (dataManager == null) { Debug.LogError("Leaderboard: DataManager is missing!"); return; }
        if (rows == null || rows.Length == 0) { Debug.LogError("Leaderboard: Rows array is empty!"); return; }


        RefreshAllData();
    }

    public void RefreshAllData()
    {
        if (headerText) headerText.text = "LOADING...";
        Debug.Log("[Leaderboard] Starting Refresh...");

        // 1. Fetch Worlds
        dataManager.RequestWorldLeaderboard((worlds) =>
        {
            _cacheWorlds = worlds;
            Debug.Log($"[Leaderboard] Worlds fetched: {worlds.Count}");

            // 2. Fetch Contributions
            dataManager.RequestPlayerLeaderboard("contribution", (contrib) =>
            {
                _cacheContrib = contrib;
                Debug.Log($"[Leaderboard] Contrib fetched: {contrib.Count}");

                // 3. Fetch Streaks
                dataManager.RequestPlayerLeaderboard("streak", (streak) =>
                {
                    _cacheStreak = streak;
                    Debug.Log($"[Leaderboard] Streak fetched: {streak.Count}");

                    // Finalize
                    RebuildFormattedLines();
                    UpdateVisuals();
                });
            });
        });
    }

    public void ChangeMode(int direction)
    {
        RefreshAllData();
        int modes = System.Enum.GetValues(typeof(BoardMode)).Length;
        int current = (int)_currentMode;
        current += direction;

        if (current >= modes) current = 0;
        if (current < 0) current = modes - 1;

        _currentMode = (BoardMode)current;
        _scrollIndex = 0; // Reset scroll

        RebuildFormattedLines();
        UpdateVisuals();
    }

    public void Scroll(int direction)
    {
        _scrollIndex += direction;
        int maxScroll = Mathf.Max(0, _formattedLines.Count - rows.Length);
        _scrollIndex = Mathf.Clamp(_scrollIndex, 0, maxScroll);
        UpdateVisuals();
    }

    private void RebuildFormattedLines()
    {
        _formattedLines.Clear();

        switch (_currentMode)
        {
            case BoardMode.PastWorlds:
                if (headerText) headerText.text = "PAST WORLDS";
                if (_cacheWorlds != null)
                {
                    foreach (var w in _cacheWorlds)
                    {
                        // UPDATE: Use PascalCase properties
                        string reason = w.resetReason.Length > 10 ? w.resetReason.Substring(0, 10) : w.resetReason.PadRight(10); 
                        _formattedLines.Add($"{reason}  DAYS:{w.daysSurvived}");
                    }
                }
                break;

            case BoardMode.Contribution:
                if (headerText) headerText.text = "TOP ENERGY";
                if (_cacheContrib != null)
                {
                    foreach (var p in _cacheContrib)
                    {
                        // UPDATE: Use PascalCase properties
                        string name = p.username.Length > 10 ? p.username.Substring(0, 10) : p.username.PadRight(10);
                        _formattedLines.Add($"{name} {FormatNumber(p.totalContribution)}");
                    }
                }
                break;

            case BoardMode.Streak:
                if (headerText) headerText.text = "LOGIN STREAK";
                if (_cacheStreak != null)
                {
                    foreach (var p in _cacheStreak)
                    {
                        // UPDATE: Use PascalCase properties
                        string name = p.username.Length > 9 ? p.username.Substring(0, 10) : p.username.PadRight(10);
                        _formattedLines.Add($"{name} {p.loginStreak}DAY");
                    }
                }
                break;
        }

        if (_formattedLines.Count == 0)
        {
            _formattedLines.Add("NO DATA");
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            int dataIndex = _scrollIndex + i;

            if (dataIndex < _formattedLines.Count)
            {
                rows[i].SetText(_formattedLines[dataIndex]);
            }
            else
            {
                rows[i].SetText("");
            }
        }
    }

    private string FormatNumber(long num)
    {
        if (num >= 1000000) return (num / 1000000D).ToString("0.0") + "M";
        if (num >= 1000) return (num / 1000D).ToString("0.0") + "K";
        return num.ToString();
    }
}