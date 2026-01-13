using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WorkstationUI : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MiniGameManager miniGameManager;
    [SerializeField] private DataManager dataManager;

    [Header("UI References")]
    [SerializeField] private TMP_Text percentageText;
    [SerializeField] private SplitFlapDisplay valueText;

    [Header("Info Display")]
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private SplitFlapDisplay daysSurvivedText;

    [Header("Nixie References")]
    [SerializeField] private TMP_Text temperatureText;
    [SerializeField] private TMP_Text flowText;
    [SerializeField] private TMP_Text rpmText;
    [SerializeField] private TMP_Text voltageText;

    [Header("Report UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text gameOverTitle;
    [SerializeField] private TMP_Text gameOverReason;
    [SerializeField] private TMP_Text gameOverStats;

    [Header("Profile Display")]
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private SplitFlapDisplay streakText;
    [SerializeField] private SplitFlapDisplay contributionText;

    [Header("Graph References")]
    [SerializeField] private LineRenderer graphLine;
    [SerializeField] private RectTransform graphContainer;

    [Header("Graph Markers")]
    [SerializeField] private GameObject markerTemplate; // Drag your 'GraphMarkerTemplate' here
    [SerializeField] private int maxLabels = 6; // Max number of timestamps to show (prevents overlapping)
    private List<GameObject> markerPool = new List<GameObject>();

    private void Start()
    {
        // Subscribe to SignalR updates for global state
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnGameStateReceived += UpdateGlobalStateUI;
            SignalRManager.Instance.OnWorldReset += HandleLiveReset;
        }

        if (gameOverPanel) gameOverPanel.SetActive(false);

        if (GameSession.Instance != null)
        {
            if (usernameText) usernameText.text = GameSession.Instance.Username;
            if (streakText) streakText.SetText($"Streak: {GameSession.Instance.LoginStreak}");
            UpdateContributionDisplay(); // Draw initial value
        }

        if (dataManager != null)
        {
            dataManager.OnTrendsReceived += UpdateTrendGraph;
        }

        // 3. Check for Offline Death (Report from Login)
        if (GameSession.Instance != null && GameSession.Instance.PendingReport != null)
        {
            var rep = GameSession.Instance.PendingReport;

            // If "reason" is null or empty, it's a ghost/glitch. DELETE IT and DO NOT show panel.
            if (!string.IsNullOrEmpty(rep.reason))
            {
                ShowGameOverScreen(
                    "FACILITY RESET WHILE OFFLINE",
                    rep.reason,
                    rep.daysSurvived
                );
            }

            // Clear the report regardless so it never persists
            GameSession.Instance.PendingReport = null;
        }
    }

    private void OnDestroy()
    {
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnGameStateReceived -= UpdateGlobalStateUI;
            SignalRManager.Instance.OnWorldReset -= HandleLiveReset;
            dataManager.OnTrendsReceived -= UpdateTrendGraph;
        }
    }


    // 2. SignalR Updates (Passive view update)
    private void UpdateGlobalStateUI(WorldStateDto state)
    {
        // SAFETY CHECK: Prevent DivideByZero if MaxWh is 0 or uninitialized
        float maxSafe = state.MaxWh > 0 ? state.MaxWh : 10000f;

        float fillAmount = (float)state.CurrentWh / maxSafe;
        float percentage = fillAmount * 100f;

        // Handle weird floating point edge cases
        if (float.IsNaN(percentage)) percentage = 0f;

        percentageText.text = $"{percentage:F3}%";
        valueText.SetText($"{state.CurrentWh}/{state.MaxWh} Wh");

        if (clockText) clockText.text = state.ServerTime;
        if (daysSurvivedText) daysSurvivedText.SetText($"DAYS SURVIVED: {state.DaysSurvived}");
    }

    // 3. MiniGame Logic Updates (Called by MiniGameManager)
    public void UpdateNixie(TaskData nixieData)
    {
        // Visual logic to make the numbers look cool/random based on the boolean flags
        temperatureText.text = nixieData.IsTempHigh ? Random.Range(350, 550).ToString() : Random.Range(150, 349).ToString();
        flowText.text = nixieData.IsFlowHigh ? "STBL" : "UNST";
        rpmText.text = nixieData.IsRPMHigh ? Random.Range(3000, 5000).ToString() : Random.Range(1000, 2999).ToString();
        voltageText.text = nixieData.IsVoltageHigh ? Random.Range(230, 300).ToString() : Random.Range(110, 229).ToString();
    }

    private void HandleLiveReset(WorldResetDto data)
    {
        // This fires immediately when the server detects < 0%
        ShowGameOverScreen("CRITICAL FAILURE", data.reason, data.daysSurvived);
    }

    private void ShowGameOverScreen(string title, string reason, int days)
    {
        if (!gameOverPanel) return;

        gameOverPanel.SetActive(true);

        // Safety Check for Title
        if (gameOverTitle)
            gameOverTitle.text = title ?? "GAME OVER";

        // Safety Check for Reason (The cause of your crash)
        if (gameOverReason)
        {
            // If reason is null, default to "UNKNOWN", then ToUpper()
            string safeReason = reason ?? "UNKNOWN CAUSE";
            gameOverReason.text = safeReason.ToUpper();
        }

        if (gameOverStats)
            gameOverStats.text = $"THE WORLD SURVIVED FOR {days} DAYS.";
    }

    public void RefreshLocalStats(long energyAdded)
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.AddContribution(energyAdded);
            UpdateContributionDisplay();
        }
    }

    private void UpdateContributionDisplay()
    {
        if (GameSession.Instance != null && contributionText)
        {
            contributionText.SetText($"{GameSession.Instance.TotalContribution:N0} Wh");
        }
    }

    private void UpdateTrendGraph(List<DataManager.BatteryHistoryPoint> history)
    {
        if (graphLine == null || graphContainer == null) return;

        graphLine.useWorldSpace = false;
        graphLine.loop = false;

        // 1. Handle No Data
        if (history == null || history.Count == 0)
        {
            graphLine.positionCount = 0;
            HideAllMarkers();
            return;
        }

        // 2. Get Dimensions & Pivot Offsets
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        float startX = -graphContainer.pivot.x * width;
        float startY = -graphContainer.pivot.y * height;
        float zDepth = -0.1f;

        // 3. Handle Single Point (Flat Line)
        if (history.Count == 1)
        {
            graphLine.positionCount = 2;
            var point = history[0];
            float max = point.maxWh > 0 ? point.maxWh : 10000f;
            float yRatio = Mathf.Clamp01((float)point.whValue / max);
            float yPos = startY + (yRatio * height);

            graphLine.SetPosition(0, new Vector3(startX, yPos, zDepth));
            graphLine.SetPosition(1, new Vector3(startX + width, yPos, zDepth));

            // Show just one marker in the middle or end
            UpdateMarkers(history, width, startX, startY);
            return;
        }

        // 4. Handle Standard Trend
        graphLine.positionCount = history.Count;
        Vector3[] positions = new Vector3[history.Count];

        for (int i = 0; i < history.Count; i++)
        {
            var point = history[i];

            // X & Y Calculation
            float xRatio = (float)i / (history.Count - 1);
            float xPos = startX + (xRatio * width);

            float max = point.maxWh > 0 ? point.maxWh : 10000f;
            float yRatio = Mathf.Clamp01((float)point.whValue / max);
            float yPos = startY + (yRatio * height);

            positions[i] = new Vector3(xPos, yPos, zDepth);
        }

        graphLine.SetPositions(positions);

        // 5. Update the Grid/Labels
        UpdateMarkers(history, width, startX, startY);
    }

    private void UpdateMarkers(List<DataManager.BatteryHistoryPoint> history, float width, float startX, float startY)
    {
        if (!markerTemplate) return;

        // A. Calculate Step (To avoid drawing 100 labels overlapping)
        // If we have 50 points and maxLabels is 5, step = 10. We draw every 10th label.
        int step = Mathf.Max(1, (history.Count - 1) / (maxLabels - 1));

        int activeMarkerCount = 0;

        for (int i = 0; i < history.Count; i++)
        {
            // Only draw marker if it matches the step (OR if it's the very last point)
            bool shouldDraw = (i % step == 0) || (i == history.Count - 1);

            if (!shouldDraw) continue;

            // B. Get or Create Marker from Pool
            GameObject marker = GetMarker(activeMarkerCount);
            marker.SetActive(true);
            activeMarkerCount++;

            // C. Position the Marker
            float xRatio = 0f;
            if (history.Count > 1)
                xRatio = (float)i / (history.Count - 1);
            else
                xRatio = 1.0f; // If single point, place at end

            float xPos = startX + (xRatio * width);

            // We set localPosition. Y is startY (bottom of container). 
            // Z is 0 (or slightly behind line if needed).
            marker.transform.localPosition = new Vector3(xPos, startY, 0);

            // D. Set Text
            // Assumes the template has a TMP_Text component in its children
            TMP_Text label = marker.GetComponentInChildren<TMP_Text>();
            if (label)
            {
                label.text = ParseTime(history[i].timestamp);
            }
        }

        // E. Hide unused markers (if we went from 10 points down to 5)
        for (int i = activeMarkerCount; i < markerPool.Count; i++)
        {
            markerPool[i].SetActive(false);
        }
    }

    // --- Helpers ---

    private GameObject GetMarker(int index)
    {
        // If we need more markers than we have in the pool, create new ones
        if (index >= markerPool.Count)
        {
            GameObject newMarker = Instantiate(markerTemplate, graphContainer);
            markerPool.Add(newMarker);
            return newMarker;
        }
        return markerPool[index];
    }

    private void HideAllMarkers()
    {
        foreach (var m in markerPool) m.SetActive(false);
    }

    private string ParseTime(string rawTime)
    {
        if (string.IsNullOrEmpty(rawTime)) return "--:--";

        // Use DateTime.TryParse for safety
        if (System.DateTime.TryParse(rawTime, out System.DateTime dt))
        {
            return dt.ToString("HH:mm");
        }
        return rawTime; // Fallback if parse fails
    }

    private void OnDrawGizmosSelected()
    {
        if (graphContainer == null) return;

        Gizmos.color = Color.green;

        // Get the corners of the RectTransform in World Space
        Vector3[] corners = new Vector3[4];
        graphContainer.GetWorldCorners(corners);

        // Draw the box
        Gizmos.DrawLine(corners[0], corners[1]); // Left
        Gizmos.DrawLine(corners[1], corners[2]); // Top
        Gizmos.DrawLine(corners[2], corners[3]); // Right
        Gizmos.DrawLine(corners[3], corners[0]); // Bottom
    }
}