using UnityEngine;
using UnityEngine.UI;

public class WaveMatchingController : MonoBehaviour
{
    [Header("Dependencies")]
    public MiniGameManager manager;
    public OscilloscopeWave playerWave;
    public OscilloscopeWave targetWave;

    // NEW: Reference to your Coil Controller
    public CoilController coilController;

    [Header("Difficulty Settings")]
    public float syncTolerance = 0.15f;
    public float lockTolerance = 0.05f;
    public float requiredSustainTime = 3.0f;

    public float _stabilityModifier = 1.0f;

    [Header("Jitter Settings")]
    [Tooltip("How fast the target values wander")]
    public float jitterSpeed = 0.5f;
    [Tooltip("How far the target values wander from their base")]
    public float jitterIntensity = 0.2f;

    [Header("Visual Feedback")]
    public Color colorFloating = Color.cyan;
    public Color colorSynced = Color.yellow;
    public Color colorLocked = Color.green;
    public Image lockIndicatorUI;

    // Internal State
    private float _currentSustainTimer = 0f;
    private bool _isSynced = false;
    private bool _canLock = false;
    private bool _isActive = false;

    // Target Behavior State
    private float _baseFreq;
    private float _baseAmp;
    private float _noiseOffsetFreq;
    private float _noiseOffsetAmp;

    void Update()
    {
        if (!_isActive) return;

        UpdateTargetBehavior();
        CheckAlignment();
    }

    public void SetStability(float modifier)
    {
        _stabilityModifier = modifier;
    }

    public void StartNewRound(int seed)
    {
        _isActive = true;
        _currentSustainTimer = 0f;

        // NEW: Reset coil visual at start of round
        if (coilController != null) coilController.SetFill(0f);

        Random.InitState(seed);

        _baseFreq = Random.Range(playerWave.minFrequency + 2f, playerWave.maxFrequency - 2f);
        _baseAmp = Random.Range(playerWave.minAmplitude + 0.2f, playerWave.maxAmplitude - 0.2f);

        _noiseOffsetFreq = Random.Range(0f, 1000f);
        _noiseOffsetAmp = Random.Range(0f, 1000f);
    }

    private void UpdateTargetBehavior()
    {
        float time = Time.time * jitterSpeed * _stabilityModifier;

        float noiseFreq = (Mathf.PerlinNoise(time + _noiseOffsetFreq, 0) - 0.5f) * 2f;
        float noiseAmp = (Mathf.PerlinNoise(0, time + _noiseOffsetAmp) - 0.5f) * 2f;

        float modJitter = jitterIntensity * _stabilityModifier;

        float targetF = _baseFreq + (noiseFreq * modJitter * 5f);
        float targetA = _baseAmp + (noiseAmp * modJitter);

        targetWave.frequency = Mathf.Clamp(targetF, targetWave.minFrequency, targetWave.maxFrequency);
        targetWave.amplitude = Mathf.Clamp(targetA, targetWave.minAmplitude, targetWave.maxAmplitude);
    }

    public void StopAndReset()
    {
        _isActive = false;
        _currentSustainTimer = 0f;
        _isSynced = false;
        _canLock = false;

        // NEW: Reset coil visual when game stops
        if (coilController != null) coilController.SetFill(0f);

        if (targetWave != null) targetWave.amplitude = 0f;
        if (playerWave != null)
        {
            playerWave.waveColor = colorFloating;
        }

        if (lockIndicatorUI != null) lockIndicatorUI.color = Color.gray;
    }

    private void CheckAlignment()
    {
        float freqError = Mathf.Abs(playerWave.frequency - targetWave.frequency) / targetWave.frequency;
        float ampError = Mathf.Abs(playerWave.amplitude - targetWave.amplitude) / targetWave.amplitude;

        float totalError = Mathf.Sqrt((freqError * freqError) + (ampError * ampError));

        _canLock = totalError < lockTolerance;
        _isSynced = totalError < syncTolerance;

        UpdateVisuals();
        UpdateSustainLogic();
    }

    private void UpdateVisuals()
    {
        if (_canLock)
        {
            playerWave.waveColor = colorLocked;
            if (lockIndicatorUI) lockIndicatorUI.color = colorLocked;
        }
        else if (_isSynced)
        {
            playerWave.waveColor = colorSynced;
            if (lockIndicatorUI) lockIndicatorUI.color = colorSynced;
        }
        else
        {
            playerWave.waveColor = colorFloating;
            if (lockIndicatorUI) lockIndicatorUI.color = Color.gray;
        }
    }

    private void UpdateSustainLogic()
    {
        // 1. Calculate Progress (0.0 to 1.0)
        // We calculate this even if not currently syncing, so the bar stays 
        // at its current height (paused) rather than snapping to 0.
        float progress = Mathf.Clamp01(_currentSustainTimer / requiredSustainTime);

        // 2. Update Coil
        if (coilController != null)
        {
            coilController.SetFill(progress);
        }

        // 3. Increment Timer only if Synced
        if (_isSynced)
        {
            _currentSustainTimer += Time.deltaTime;

            if (_currentSustainTimer >= requiredSustainTime)
            {
                CompleteTask(false);
            }
        }
        // Note: We do NOT reset timer to 0 if they lose sync. 
        /*
        else 
        {
             // Optional: Decay the timer or reset it?
             // _currentSustainTimer -= Time.deltaTime; 
             // _currentSustainTimer = Mathf.Max(0, _currentSustainTimer);
        } 
        */
    }

    public void LockWave()
    {
        if (_canLock)
            CompleteTask(true);
    }

    private void CompleteTask(bool isBonus)
    {
        if (!_isActive) return;

        // Optional: Fill coil completely on win
        if (coilController != null) coilController.SetFill(1f);

        _isActive = false;
        Debug.Log(isBonus ? "LOCKED IN! (Bonus)" : "SUSTAINED! (Normal)");

        manager.OnTaskSolved(isBonus);
    }
}