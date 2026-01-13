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

    [Header("Keyboard Control")]
    [Tooltip("How many degrees to rotate per Input Event trigger.")]
    [SerializeField] private float keyStepAmount = 5f;

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

        if (knobHandle != null)
        {
            _defaultRotation = knobHandle.localRotation;
        }

        if (!rotaryEncoder)
        {
            _currentAngle = minAngle;
        }
        else
        {
            _currentAngle = 0f;
        }

        UpdateVisuals(_currentAngle);
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
        // 1. Handle Mouse Input
        // We only calculate delta if we are currently interacting with the mouse
        if (_isInteracting && knobHandle != null)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 direction = mousePos - _screenPosition;

            float currentMouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float frameDelta = Mathf.DeltaAngle(_lastMouseAngle, currentMouseAngle);

            _lastMouseAngle = currentMouseAngle;

            if (invertInput) frameDelta *= -1f;

            // In your original logic, you subtracted the delta.
            // We pass negative frameDelta to maintain that behavior.
            ApplyRotationDelta(-frameDelta);
        }
    }

    // --- PUBLIC METHODS FOR PLAYER INPUT CONTROLLER ---

    // Call this to rotate towards positive/max
    public void RotateIncrease()
    {
        ApplyRotationDelta(keyStepAmount);
    }

    // Call this to rotate towards negative/min
    public void RotateDecrease()
    {
        ApplyRotationDelta(-keyStepAmount);
    }

    // --- CORE LOGIC ---

    // This method is now the single source of truth for updating the knob
    private void ApplyRotationDelta(float delta)
    {
        if (knobHandle == null) return;

        // Apply Delta
        _currentAngle += delta;

        // Clamp Logic (Only for Non-Rotary)
        if (!rotaryEncoder)
        {
            _currentAngle = Mathf.Clamp(_currentAngle, minAngle, maxAngle);
        }

        // Visuals
        UpdateVisuals(_currentAngle);

        // Logic
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

        if (rotaryEncoder)
        {
            stepSize = 360f / steps;
        }
        else
        {
            float range = maxAngle - minAngle;
            stepSize = range / Mathf.Max(1, steps - 1);
        }

        int currentStepIndex = Mathf.RoundToInt((rotaryEncoder ? _currentAngle : (_currentAngle - minAngle)) / stepSize);

        if (currentStepIndex != _lastStepIndex)
        {
            // Optional: PlaySound(0); 

            float valueToSend;

            if (rotaryEncoder)
            {
                valueToSend = (float)_lastStepIndex - currentStepIndex;
            }
            else
            {
                float snappedAngle = (currentStepIndex * stepSize) + minAngle;
                valueToSend = Mathf.InverseLerp(minAngle, maxAngle, snappedAngle);
            }

            _lastStepIndex = currentStepIndex;
            OnValueChanged?.Invoke(valueToSend);
        }
    }

    private void HandleSmoothLogic()
    {
        float valueToSend;

        if (rotaryEncoder)
        {
            valueToSend = _currentAngle;
        }
        else
        {
            valueToSend = Mathf.InverseLerp(minAngle, maxAngle, _currentAngle);
        }

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
}