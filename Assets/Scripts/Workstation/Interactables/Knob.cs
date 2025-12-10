using UnityEngine;
using UnityEngine.Events;

public class Knob : BaseInteractable
{
    [Header("Knob Settings")]
    [SerializeField] private Transform knobHandle;

    [Tooltip("Which local axis should the knob rotate around?")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;

    [Header("Limits")]
    [Tooltip("If true, the knob spins infinitely (Min/Max are ignored).")]
    [SerializeField] private bool rotaryEncoder = false;

    [SerializeField] private float minAngle = -135f;
    [SerializeField] private float maxAngle = 135f;

    [Header("Steps")]
    [Tooltip("0 = Continuous Event. >0 = Event fires only when a step is crossed.")]
    [SerializeField] private int steps = 0;

    [Tooltip("Check this if rotating right makes the value go down.")]
    [SerializeField] private bool invertInput = false;

    [Header("Events")]
    // Normal Mode: Returns 0.0 to 1.0
    // Rotary Mode: Returns the current Step Index (e.g. 5.0, 6.0, 7.0)
    public UnityEvent<float> OnValueChanged;

    // State Variables
    private float _currentAngle; // The raw accumulated angle
    private Vector2 _screenPosition;
    private float _lastMouseAngle;
    private Quaternion _defaultRotation;

    // Tracking for steps
    private int _lastStepIndex = 0;
    private float _lastEmittedValue = -1f;

    public enum RotationAxis { X, Y, Z }

    protected override void Start()
    {
        base.Start();

        // 1. Remember the rotation exactly as it was placed in the Scene
        if (knobHandle != null)
        {
            _defaultRotation = knobHandle.localRotation;
        }

        // 2. Set initial state
        if (!rotaryEncoder)
        {
            _currentAngle = minAngle;
        }
        else
        {
            _currentAngle = 0f;
        }

        UpdateVisuals();
    }

    public override void OnInteractStart()
    {
        base.OnInteractStart();

        if (knobHandle == null) return;

        _screenPosition = _mainCamera.WorldToScreenPoint(knobHandle.position);

        Vector2 mousePos = Input.mousePosition;
        Vector2 direction = mousePos - _screenPosition;
        _lastMouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    protected override void Update()
    {
        if (!_isInteracting || knobHandle == null) return;

        // --- 1. Input Math (Get Smooth Delta) ---
        Vector2 mousePos = Input.mousePosition;
        Vector2 direction = mousePos - _screenPosition;

        float currentMouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float frameDelta = Mathf.DeltaAngle(_lastMouseAngle, currentMouseAngle);

        _lastMouseAngle = currentMouseAngle;

        if (invertInput) frameDelta *= -1f;

        // Apply Delta
        _currentAngle -= frameDelta;

        // --- 2. Clamp Logic (Only for Non-Rotary) ---
        if (!rotaryEncoder)
        {
            _currentAngle = Mathf.Clamp(_currentAngle, minAngle, maxAngle);
        }

        // --- 3. Visuals (ALWAYS Smooth) ---
        UpdateVisuals(_currentAngle);

        // --- 4. Step & Event Logic ---
        if (steps > 0)
        {
            HandleSteppedLogic();
        }
        else
        {
            HandleSmoothLogic();
        }
    }

    private void HandleSteppedLogic()
    {
        float stepSize;

        // Calculate Step Size based on mode
        if (rotaryEncoder)
        {
            // For rotary, steps = "How many clicks in one full 360 rotation"
            stepSize = 360f / steps;
        }
        else
        {
            // For limited, steps = "How many clicks from Min to Max"
            // We use (steps - 1) because if you have 2 steps, you have one gap.
            // Example: Min 0, Max 100, 2 Steps -> Step 0 (0) and Step 1 (100). Size is 100.
            float range = maxAngle - minAngle;
            stepSize = range / Mathf.Max(1, steps - 1);
        }

        // Calculate which "Step Index" we are currently on using Round (Nearest Neighbor)
        // For Rotary, this can go negative or above steps (infinite)
        // For Limited, this is effectively clamped by the _currentAngle clamp earlier
        int currentStepIndex = Mathf.RoundToInt((rotaryEncoder ? _currentAngle : (_currentAngle - minAngle)) / stepSize);

        // Only fire event if the index CHANGED
        if (currentStepIndex != _lastStepIndex)
        {
            _lastStepIndex = currentStepIndex;
            PlaySound(0);

            float valueToSend;

            if (rotaryEncoder)
            {
                // In Rotary mode, we just send the Index. 
                // The receiver can check (newValue > oldValue) to know direction.
                valueToSend = (float)currentStepIndex;
            }
            else
            {
                // In Limited mode, we send the normalized 0-1 value of the SNAP point
                float snappedAngle = (currentStepIndex * stepSize) + minAngle;
                valueToSend = Mathf.InverseLerp(minAngle, maxAngle, snappedAngle);
            }

            OnValueChanged?.Invoke(valueToSend);
        }
    }

    private void HandleSmoothLogic()
    {
        float valueToSend;

        if (rotaryEncoder)
        {
            // In smooth rotary mode, maybe we just send the raw angle? 
            // Or usually normalized 0-1 within 360? 
            // Let's send the raw angle for maximum flexibility.
            valueToSend = _currentAngle;
        }
        else
        {
            // Standard 0-1 Normalization
            valueToSend = Mathf.InverseLerp(minAngle, maxAngle, _currentAngle);
        }

        // Check threshold to prevent event spam
        if (Mathf.Abs(valueToSend - _lastEmittedValue) > 0.001f)
        {
            _lastEmittedValue = valueToSend;
            OnValueChanged?.Invoke(valueToSend);
        }
    }

    private void UpdateVisuals(float angleToRot)
    {
        Vector3 axisVector = Vector3.forward;
        switch (rotationAxis)
        {
            case RotationAxis.X: axisVector = Vector3.right; break;
            case RotationAxis.Y: axisVector = Vector3.up; break;
            case RotationAxis.Z: axisVector = Vector3.forward; break;
        }

        knobHandle.localRotation = _defaultRotation * Quaternion.AngleAxis(angleToRot, axisVector);
    }

    private void UpdateVisuals()
    {
        UpdateVisuals(_currentAngle);
    }
}