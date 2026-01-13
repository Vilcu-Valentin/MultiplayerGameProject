using System.Threading.Tasks;
using UnityEngine;
using Microsoft.AspNetCore.SignalR.Client;
using System;

public class SignalRManager : MonoBehaviour
{
    public static SignalRManager Instance { get; private set; }

    [Header("Connection Settings")]
    [SerializeField] private string serverUrl = "http://localhost:5000/gameHub"; // Ensure this matches your Hub endpoint

    private HubConnection _connection;

    // Event that UI scripts can subscribe to
    public event Action<WorldStateDto> OnGameStateReceived;
    public event Action<WorldResetDto> OnWorldReset;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        // Wait for the GameSession to have a token (in case of race conditions on scene load)
        while (GameSession.Instance == null || string.IsNullOrEmpty(GameSession.Instance.AuthToken))
        {
            await Task.Delay(100);
        }

        await InitializeSignalR();
    }

    private async Task InitializeSignalR()
    {
        Debug.Log($"Connecting to SignalR at {serverUrl}...");

        _connection = new HubConnectionBuilder()
            .WithUrl(serverUrl, options =>
            {
                // Attach the JWT token to the connection headers
                options.AccessTokenProvider = () => Task.FromResult(GameSession.Instance.AuthToken);
            })
            .WithAutomaticReconnect()
            .Build();

        // --- REGISTER HANDLERS ---

        // Listen for "ReceiveWorldState" from the Server (GameController.cs line 48)
        _connection.On<WorldStateDto>("ReceiveWorldState", (state) =>
        {
            // SignalR runs on a background thread. 
            // We must dispatch this to the Main Thread to update Unity UI.
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                Debug.Log($"[SignalR] Received Update: {state.CurrentWh}/{state.MaxWh}");
                OnGameStateReceived?.Invoke(state);
            });
        });

        _connection.On<WorldResetDto>("OnWorldReset", (resetData) =>
        {
            MainThreadDispatcher.Instance.Enqueue(() =>
            {
                Debug.Log($"<color=red>GAME OVER RECEIVED! Reason: {resetData.reason}</color>");
                OnWorldReset?.Invoke(resetData);
            });
        });

        // --- START CONNECTION ---

        try
        {
            await _connection.StartAsync();
            Debug.Log("SignalR Connected!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"SignalR Connection Error: {ex.Message}");
        }
    }

    private async void OnDestroy()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}