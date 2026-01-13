using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class MiniGameManager : MonoBehaviour
{
    [Header("Network Configuration")]
    [SerializeField] private string baseUrl = "http://localhost:5000/api/MiniGame";

    [Header("Game Controllers")]
    [SerializeField] private WaveMatchingController waveGame;
    [SerializeField] private CalibrationController calibrationManager;
    [SerializeField] private WorkstationUI workstation;

    [Header("Settings")]
    [SerializeField] private float gameDurationSeconds = 120f; // 2 Minutes

    // Internal State
    private TaskGenerator _taskGen;
    private int _tasksCompleted = 0;
    private int _bonusTasks = 0;
    private string _intention = "CHARGE";

    // Session State
    private string _currentMiniGameId;
    private int _currentSeed;
    private bool _isGameActive = false;
    private float _timeRemaining;

    // --- Unity Lifecycle ---

    private void Update()
    {
        if (_isGameActive)
        {
            _timeRemaining -= Time.deltaTime;

            // Optional: Debug every 10 seconds to show time ticking
            if (Mathf.FloorToInt(_timeRemaining) % 10 == 0 && Mathf.FloorToInt(_timeRemaining) != Mathf.FloorToInt(_timeRemaining + Time.deltaTime))
            {
                Debug.Log($"Time Remaining: {_timeRemaining:F0}s");
            }

            if (_timeRemaining <= 0)
            {
                Debug.Log("<color=red>TIME'S UP!</color>");
                FinishGame();
            }
        }
    }

    // --- Game Flow ---

    public void RequestStartGame()
    {
        if (_isGameActive) return; // Prevent double start
        StartCoroutine(StartGameRoutine(_intention));
    }

    public void SetGameIntention(int index)
    {
        if (index == 0)
            _intention = "CHARGE";
        else
            _intention = "DISCHARGE";
    }    

    private IEnumerator StartGameRoutine(string intention)
    {
        // 1. Prepare Request
        var reqData = new MiniGameStartRequest { Intention = intention };
        string json = JsonUtility.ToJson(reqData);

        using (UnityWebRequest uwr = new UnityWebRequest($"{baseUrl}/start", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            if (GameSession.Instance != null)
                uwr.SetRequestHeader("Authorization", "Bearer " + GameSession.Instance.AuthToken);

            Debug.Log("Requesting MiniGame Session...");
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to start game: {uwr.error} : {uwr.downloadHandler.text}");
            }
            else
            {
                var res = JsonUtility.FromJson<MiniGameStartResponse>(uwr.downloadHandler.text);
                _currentMiniGameId = res.miniGameId;
                _currentSeed = res.seed;

                Debug.Log($"<color=green>Session Started! ID: {_currentMiniGameId}, Seed: {_currentSeed}</color>, Intention: {intention}");

                BeginGameplayLoop(_currentSeed);
            }
        }
    }

    private void BeginGameplayLoop(int seed)
    {
        _taskGen = new TaskGenerator(seed);
        _tasksCompleted = 0;
        _bonusTasks = 0;

        // Reset Timer
        _timeRemaining = gameDurationSeconds;
        _isGameActive = true;

        // Start first task
        LoadNextTask();
    }

    public void LoadNextTask()
    {
        if (!_isGameActive) return;

        // 1. Get Visuals for Nixie
        TaskData newTask = _taskGen.GetNextTask();
        calibrationManager.OnNewTaskStarted(newTask);

        if (workstation != null)
        {
            workstation.UpdateNixie(newTask);
        }

        // 2. Start the Wave Game logic
        // We use a deterministic seed offset so the wave pattern is reproducible if needed
        int taskSeed = _currentSeed + _tasksCompleted;
        waveGame.StartNewRound(taskSeed);

        Debug.Log($"<color=cyan>Task #{_tasksCompleted + 1} Started. (Time: {_timeRemaining:F0}s)</color>");
    }

    public void OnTaskSolved(bool isBonus)
    {
        if (!_isGameActive) return;

        _tasksCompleted++;
        if (isBonus) _bonusTasks++;

        LoadNextTask();
    }

    public void FinishGame()
    {
        if (!_isGameActive) return;

        _isGameActive = false;

        // --- UPDATED: Properly reset the wave game instead of just disabling it ---
        if (waveGame != null)
        {
            waveGame.StopAndReset();
        }

        Debug.Log($"Game Over! Submitting to server... Completed: {_tasksCompleted} (Bonus: {_bonusTasks})");
        StartCoroutine(SubmitResultRoutine());
    }

    private IEnumerator SubmitResultRoutine()
    {
        var reqData = new MiniGameSubmitRequest
        {
            MiniGameId = _currentMiniGameId,
            Seed = _currentSeed,
            TasksCompleted = _tasksCompleted,
            BonusTasks = _bonusTasks
        };

        string json = JsonUtility.ToJson(reqData);

        using (UnityWebRequest uwr = new UnityWebRequest($"{baseUrl}/submit", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            if (GameSession.Instance != null)
                uwr.SetRequestHeader("Authorization", "Bearer " + GameSession.Instance.AuthToken);

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Submission Failed: {uwr.error} : {uwr.downloadHandler.text}");
            }
            else
            {
                var res = JsonUtility.FromJson<MiniGameSubmitResponse>(uwr.downloadHandler.text);
                workstation.RefreshLocalStats(res.energyApplied);

                if (res.efficiency < 1.0f)
                {
                    Debug.LogWarning($"Grind Penalty Active! Efficiency: {res.efficiency * 100}%");
                    // TODO: Show UI Popup: "System Overloaded - Efficiency Reduced"
                }

                Debug.Log($"<color=green>Success! Applied {res.energyApplied}Wh. New Total: {res.newBatteryTotal}</color>");
            }
        }
    }
}