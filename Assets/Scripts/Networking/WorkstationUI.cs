using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class WorkstationUI : MonoBehaviour
{
    [Header("Server API")]
    [SerializeField] private string baseUrl = "http://localhost:5000/api/Game/submitCharge";

    [Header("UI References")]
    [SerializeField] private Image batteryGauge; // Assign a Filled Type Image here
    [SerializeField] private TMP_Text percentageText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text statusText; // For error/success messages

    private void Start()
    {
        // Subscribe to SignalR updates
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnGameStateReceived += UpdateUI;
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent errors when scene closes
        if (SignalRManager.Instance != null)
        {
            SignalRManager.Instance.OnGameStateReceived -= UpdateUI;
        }
    }

    /// <summary>
    /// Called by SignalR (via the Manager) whenever the server sends a broadcast.
    /// </summary>
    private void UpdateUI(WorldStateDto state)
    {
        float fillAmount = (float)state.CurrentWh / state.MaxWh;
        //batteryGauge.fillAmount = fillAmount;

        float percentage = fillAmount * 100f;
        percentageText.text = $"{percentage:F3}%";

        valueText.text = $"{state.CurrentWh} /\n{state.MaxWh}";

        // Optional: Change color based on low battery
        //batteryGauge.color = fillAmount < 0.2f ? Color.red : Color.green;
    }

    // --- BUTTON HANDLERS ---

    // Hook this to the "Charge 400Wh" Button
    public void OnChargeButtonClicked()
    {
        StartCoroutine(SendChargeRequest(400));
    }

    // Hook this to the "Discharge 300Wh" Button
    public void OnDischargeButtonClicked()
    {
        StartCoroutine(SendChargeRequest(-300));
    }

    private IEnumerator SendChargeRequest(int amount)
    {
        //statusText.text = "Sending...";

        // Create JSON Payload
        string jsonPayload = $"{{\"amount\":{amount}}}"; // simple manual JSON for single field
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest uwr = new UnityWebRequest(baseUrl, "POST"))
        {
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            uwr.SetRequestHeader("Content-Type", "application/json");

            // *** CRITICAL: Add the Authorization Header ***
            uwr.SetRequestHeader("Authorization", "Bearer " + GameSession.Instance.AuthToken);

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                //statusText.text = $"Error: {uwr.responseCode}";
                Debug.LogError($"Charge Error: {uwr.error} - {uwr.downloadHandler.text}");
            }
            else
            {
                //statusText.text = "Request Sent";
                // Note: We do NOT update the UI here. We wait for the SignalR update to come back.
                // This ensures the UI is always in sync with the server's truth.
            }
        }
    }
}