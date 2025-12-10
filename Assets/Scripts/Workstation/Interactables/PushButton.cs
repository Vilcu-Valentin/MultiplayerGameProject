using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
public class WorldButton : BaseInteractable
{
    [Header("Motion Settings")]
    [Tooltip("How far the button pushes in.")]
    [SerializeField] private float pressDistance = 0.05f;

    [Tooltip("Which local axis does the button push along?")]
    [SerializeField] private PushAxis pushAxis = PushAxis.Y;

    [Tooltip("If true, pushes in the negative direction (e.g. Down instead of Up).")]
    [SerializeField] private bool invertDirection = true;

    [SerializeField] private float pressDuration = 0.1f;

    [Header("Events")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    // State
    private Vector3 _initialPosition;
    private Quaternion _initialRotation; // We store this to calculate the push vector correctly
    private Coroutine _moveRoutine;

    public enum PushAxis { X, Y, Z }

    protected override void Start()
    {
        base.Start();
        // 1. Capture the exact start position and rotation
        _initialPosition = transform.localPosition;
        _initialRotation = transform.localRotation;
    }

    public override void OnInteractStart()
    {
        base.OnInteractStart();

        PlaySound(0); // Play "Press" sound
        onPressed?.Invoke();

        // 2. Calculate target position relative to SELF, not Parent
        // We take the local axis vector, rotate it by the button's rotation, and multiply by distance.
        Vector3 pushDirection = GetAxisVector();
        Vector3 localOffset = _initialRotation * pushDirection * pressDistance;

        MoveTo(_initialPosition + localOffset);
    }

    public override void OnInteractEnd()
    {
        base.OnInteractEnd();

        PlaySound(1); // Play "Release" sound
        onReleased?.Invoke();

        MoveTo(_initialPosition);
    }

    // Safety: If mouse leaves while holding, release the button
    public override void OnHoverExit()
    {
        base.OnHoverExit();
        if (_isInteracting) OnInteractEnd();
    }

    private void MoveTo(Vector3 targetPos)
    {
        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveRoutine(targetPos));
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        float t = 0;
        Vector3 start = transform.localPosition;

        while (t < pressDuration)
        {
            t += Time.deltaTime;
            float p = t / pressDuration;
            // EaseOutQuad for a nicer "mechanical" feel
            p = 1 - (1 - p) * (1 - p);

            transform.localPosition = Vector3.Lerp(start, target, p);
            yield return null;
        }
        transform.localPosition = target;
    }

    private Vector3 GetAxisVector()
    {
        Vector3 axis = Vector3.up;
        switch (pushAxis)
        {
            case PushAxis.X: axis = Vector3.right; break;
            case PushAxis.Y: axis = Vector3.up; break;
            case PushAxis.Z: axis = Vector3.forward; break;
        }
        return invertDirection ? -axis : axis;
    }
}