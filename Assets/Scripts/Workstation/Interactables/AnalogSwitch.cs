using UnityEngine;
using UnityEngine.Events;

public class AnalogSwitch : BaseInteractable
{
    [Header("Switch Settings")]
    [Tooltip("The parent object that acts as the pivot point. If null, will try to use transform.parent.")]
    [SerializeField] private Transform pivotTransform;

    [Tooltip("Which local axis of the PIVOT should act as the axis of rotation?")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;

    [Header("Arc Configuration")]
    [SerializeField] private float minAngle = -45f;
    [SerializeField] private float maxAngle = 45f;

    [Range(2, 32)]
    [SerializeField] private int positionCount = 2;

    [Header("Feel")]
    [SerializeField] private float lerpSpeed = 10f;
    [Tooltip("Check this if moving the mouse right/up should decrease the value.")]
    [SerializeField] private bool invertInput = false;

    [Header("Events")]
    // Invoked when the switch snaps to a new index (0 to positionCount - 1)
    public UnityEvent<int> OnValueChanged;

    // State Variables
    private int _currentIndex = 0;
    private float _virtualAngle; // The raw calculated angle from mouse input
    private float _visualTargetAngle; // The angle the mesh should be lerping towards

    // Input Tracking
    private Vector2 _pivotScreenPosition;
    private float _lastMouseAngle;
    private Quaternion _defaultPivotRotation;

    public enum RotationAxis { X, Y, Z }

    public int CurrentIndex => _currentIndex;

    protected override void Start()
    {
        base.Start();

        // 1. Setup Pivot
        if (pivotTransform == null)
        {
            pivotTransform = transform.parent;
        }

        if (pivotTransform == null)
        {
            Debug.LogError($"[AnalogSwitch] {name} requires a parent object to act as a Pivot!", this);
            enabled = false;
            return;
        }

        _defaultPivotRotation = pivotTransform.localRotation;

        // 2. Initialize State
        // We start at the min angle (Index 0) or you could add logic to start elsewhere
        _virtualAngle = minAngle;
        _visualTargetAngle = minAngle;

        UpdateRotation(true); // Immediate snap on start
    }

    public override void OnInteractStart()
    {
        base.OnInteractStart();

        if (pivotTransform == null) return;

        // Calculate Pivot Screen Position (Center of rotation)
        _pivotScreenPosition = _mainCamera.WorldToScreenPoint(pivotTransform.position);

        // Calculate initial mouse offset
        Vector2 mousePos = Input.mousePosition;
        Vector2 direction = mousePos - _pivotScreenPosition;
        _lastMouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    protected override void Update()
    {
        // 1. Handle Input (Only when interacting)
        if (_isInteracting && pivotTransform != null)
        {
            HandleMouseInput();
        }

        // 2. Handle Visuals (Always, to allow lerping after release)
        UpdateRotation(false);
    }

    private void HandleMouseInput()
    {
        Vector2 mousePos = Input.mousePosition;
        Vector2 direction = mousePos - _pivotScreenPosition;

        float currentMouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float frameDelta = Mathf.DeltaAngle(_lastMouseAngle, currentMouseAngle);

        _lastMouseAngle = currentMouseAngle;

        if (invertInput) frameDelta *= -1f;

        // Apply delta to our "Virtual" non-snapped angle
        // We subtract delta because dragging clockwise (negative angle) usually means "increasing" index in Unity UI,
        // but physically dragging "up" (positive angle) should increase. 
        // NOTE: Adjust sign based on your specific camera setup preference.
        // Assuming Standard: Dragging Right/Up (Positive) -> Increases Angle.
        _virtualAngle += frameDelta;

        // Clamp the virtual angle roughly within bounds (with a little buffer for feel)
        // This prevents the user from winding the mouse 360 degrees away
        float buffer = 10f;
        _virtualAngle = Mathf.Clamp(_virtualAngle, minAngle - buffer, maxAngle + buffer);

        // Calculate Step Logic
        ProcessStepLogic();
    }

    private void ProcessStepLogic()
    {
        // Calculate the step size
        float range = maxAngle - minAngle;
        float stepSize = range / Mathf.Max(1, positionCount - 1);

        // Determine which index allows the snapped angle to be closest to our virtual angle
        // Formula: (Current - Min) / StepSize
        float rawIndex = (_virtualAngle - minAngle) / stepSize;
        int newIndex = Mathf.RoundToInt(rawIndex);

        // Clamp Index
        newIndex = Mathf.Clamp(newIndex, 0, positionCount - 1);

        // If the index changed, fire event and update target
        if (newIndex != _currentIndex)
        {
            _currentIndex = newIndex;

            // Calculate where the switch *physically* should be for this index
            _visualTargetAngle = minAngle + (_currentIndex * stepSize);

            // Audio / Events
            // PlaySound(0); // Optional: Trigger sound on click
            OnValueChanged?.Invoke(_currentIndex);
        }
    }

    private void UpdateRotation(bool immediate)
    {
        if (pivotTransform == null) return;

        // Get the current local rotation of the pivot
        Quaternion currentRot = pivotTransform.localRotation;

        // Calculate target rotation based on axis
        Vector3 axisVector = Vector3.forward;
        switch (rotationAxis)
        {
            case RotationAxis.X: axisVector = Vector3.right; break;
            case RotationAxis.Y: axisVector = Vector3.up; break;
            case RotationAxis.Z: axisVector = Vector3.forward; break;
        }

        Quaternion targetRot = _defaultPivotRotation * Quaternion.AngleAxis(_visualTargetAngle, axisVector);

        if (immediate)
        {
            pivotTransform.localRotation = targetRot;
        }
        else
        {
            // Smoothly interpolate
            pivotTransform.localRotation = Quaternion.Lerp(pivotTransform.localRotation, targetRot, Time.deltaTime * lerpSpeed);
        }
    }

    // --- External / API Methods ---

    public void SetIndex(int index)
    {
        index = Mathf.Clamp(index, 0, positionCount - 1);
        _currentIndex = index;

        float range = maxAngle - minAngle;
        float stepSize = range / Mathf.Max(1, positionCount - 1);

        _visualTargetAngle = minAngle + (_currentIndex * stepSize);
        _virtualAngle = _visualTargetAngle; // Reset virtual drag position to match

        OnValueChanged?.Invoke(_currentIndex);
    }
}