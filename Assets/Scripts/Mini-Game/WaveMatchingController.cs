using UnityEngine;
using UnityEngine.UI;

public class WaveMatchingController : MonoBehaviour
{
    [Header("Dependencies")]
    public MiniGameManager manager;
    public OscilloscopeWave playerWave;
    public OscilloscopeWave targetWave;

    [Header("Difficulty Settings")]
    public float syncTolerance = 0.15f; // 15% difference allowed for Sync
    public float lockTolerance = 0.05f; // 5% difference allowed for Lock
    public float requiredSustainTime = 3.0f;

    public float _stabilityModifier = 1.0f; // 1.0 = Normal, 0.2 = Easy

    [Header("Jitter Settings")]
    [Tooltip("How fast the target values wander")]
    public float jitterSpeed = 0.5f;
    [Tooltip("How far the target values wander from their base")]
    public float jitterIntensity = 0.2f;

    [Header("Visual Feedback")]
    public Color colorFloating = Color.cyan;
    public Color colorSynced = Color.yellow;
    public Color colorLocked = Color.green;
    public Image lockIndicatorUI; // Optional: UI icon that lights up when lock is available

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

        Random.InitState(seed);

        // Pick a "Center" point for this round
        _baseFreq = Random.Range(playerWave.minFrequency + 2f, playerWave.maxFrequency - 2f);
        _baseAmp = Random.Range(playerWave.minAmplitude + 0.2f, playerWave.maxAmplitude - 0.2f);

        // Random offsets so Perlin noise doesn't look identical every time
        _noiseOffsetFreq = Random.Range(0f, 1000f);
        _noiseOffsetAmp = Random.Range(0f, 1000f);
    }

    private void UpdateTargetBehavior()
    {
        float time = Time.time * jitterSpeed * _stabilityModifier;

        // Perlin noise returns 0.0 to 1.0. We map it to -1 to 1 for directionality.
        float noiseFreq = (Mathf.PerlinNoise(time + _noiseOffsetFreq, 0) - 0.5f) * 2f;
        float noiseAmp = (Mathf.PerlinNoise(0, time + _noiseOffsetAmp) - 0.5f) * 2f;

        // Apply Jitter
        float modJitter = jitterIntensity * _stabilityModifier; // Less intensity

        float targetF = _baseFreq + (noiseFreq * modJitter * 5f);
        float targetA = _baseAmp + (noiseAmp * modJitter);

        // Clamp to safe limits
        targetWave.frequency = Mathf.Clamp(targetF, targetWave.minFrequency, targetWave.maxFrequency);
        targetWave.amplitude = Mathf.Clamp(targetA, targetWave.minAmplitude, targetWave.maxAmplitude);
    }

    public void StopAndReset()
    {
        _isActive = false;
        _currentSustainTimer = 0f;
        _isSynced = false;
        _canLock = false;

        // Visual Reset: Flatline the waves so they don't look frozen
        if (targetWave != null) targetWave.amplitude = 0f;
        if (playerWave != null)
        {
            playerWave.waveColor = colorFloating; // Reset color
        }

        if (lockIndicatorUI != null) lockIndicatorUI.color = Color.gray;
    }

    // --- UPDATED: Math for Combined Tolerance ---
    private void CheckAlignment()
    {
        // 1. Calculate Relative Errors (0.0 to 1.0+)
        // We use Abs() to get the magnitude of difference
        float freqError = Mathf.Abs(playerWave.frequency - targetWave.frequency) / targetWave.frequency;
        float ampError = Mathf.Abs(playerWave.amplitude - targetWave.amplitude) / targetWave.amplitude;

        // 2. Calculate Euclidean Distance (Combined Error Vector)
        // Formula: Sqrt(freqError^2 + ampError^2)
        // This creates a circular "sweet spot" rather than a square one.
        float totalError = Mathf.Sqrt((freqError * freqError) + (ampError * ampError));

        // 3. Check Logic against Total Error
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
        if (_isSynced)
        {
            // Count up
            _currentSustainTimer += Time.deltaTime;

            // Check Win
            if (_currentSustainTimer >= requiredSustainTime)
            {
                CompleteTask(false); // Normal Win
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

        _isActive = false; // Prevent double completion
        Debug.Log(isBonus ? "LOCKED IN! (Bonus)" : "SUSTAINED! (Normal)");

        manager.OnTaskSolved(isBonus);
    }
}