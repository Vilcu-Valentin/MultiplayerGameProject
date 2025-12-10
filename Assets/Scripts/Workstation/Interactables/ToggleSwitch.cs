using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class ToggleSwitch : BaseInteractable
{
    [Header("Toggle Settings")]
    public bool isOn;
    [SerializeField] private bool isRadioGroup = false;
    [SerializeField] private List<ToggleSwitch> linkedToggles;

    [Header("Motion Settings")]
    [SerializeField] private Transform switchMesh;

    [Tooltip("Axis to rotate around (relative to the mesh's start rotation).")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.X;

    [Tooltip("Angle when Switch is ON.")]
    [SerializeField] private float onAngle = 45f;

    [Tooltip("Angle when Switch is OFF.")]
    [SerializeField] private float offAngle = -45f;

    [Header("Events")]
    public UnityEvent<bool> OnToggle;

    // State
    private Quaternion _defaultRotation; // The rotation from the Editor
    public enum RotationAxis { X, Y, Z }

    protected override void Start()
    {
        base.Start();

        // 1. Capture the initial rotation of the visual mesh
        if (switchMesh != null)
        {
            _defaultRotation = switchMesh.localRotation;
        }
        else
        {
            // Fallback if user forgot to assign mesh (assumes script is on the mesh)
            switchMesh = transform;
            _defaultRotation = transform.localRotation;
        }

        // Initialize state without playing sounds/events
        UpdateVisuals();
    }

    public override void OnInteractStart()
    {
        base.OnInteractStart();

        // Radio Logic: If we are already ON, don't turn OFF.
        if (isRadioGroup && isOn) return;

        SetState(!isOn);
    }

    public void SetState(bool state, bool silent = false)
    {
        isOn = state;

        UpdateVisuals();

        if (!silent)
        {
            PlaySound(isOn ? 0 : 1);
            OnToggle?.Invoke(isOn);

            // Handle Linked Radio Buttons
            if (isOn && linkedToggles.Count > 0)
            {
                foreach (var toggle in linkedToggles)
                {
                    if (toggle != this && toggle.isOn)
                    {
                        // Turn others off silently so we don't spam events/sounds
                        toggle.SetState(false, true);
                    }
                }
            }
        }
    }

    private void UpdateVisuals()
    {
        if (switchMesh == null) return;

        Vector3 axisVector = Vector3.right;
        switch (rotationAxis)
        {
            case RotationAxis.X: axisVector = Vector3.right; break;
            case RotationAxis.Y: axisVector = Vector3.up; break;
            case RotationAxis.Z: axisVector = Vector3.forward; break;
        }

        // 2. Rotate relative to the default rotation
        // We calculate the target offset angle (e.g. +45 or -45)
        float targetAngle = isOn ? onAngle : offAngle;

        // Combine the default rotation with the new offset
        switchMesh.localRotation = _defaultRotation * Quaternion.AngleAxis(targetAngle, axisVector);
    }
}