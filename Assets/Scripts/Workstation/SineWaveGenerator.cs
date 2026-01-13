using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class OscilloscopeWave : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveLength = 10f;    // Total width of the view
    public float amplitude = 1f;      // Height of the wave
    public float frequency = 1f;      // Cycles per second
    public float speed = 5f;          // How fast the wave scrolls left
    public float verticalShift = 0f;  // Move the baseline up/down

    [Header("Bounding Box (Clipping)")]
    [Tooltip("If the wave exceeds these Y values, it will flatten (clip).")]
    public float yMax = 3f;
    public float yMin = -3f;

    [Header("Quality")]
    public int resolution = 100;      // Number of points in the history buffer

    [Header("Appearance")]
    public float thickness = 0.1f;
    public Color waveColor = Color.cyan;

    [Header("MiniGame")]
    public float minAmplitude = 0f;
    public float maxAmplitude = 1f;

    public float minFrequency = 0.1f;
    public float maxFrequency = 20f;

    private LineRenderer lineRenderer;

    // We store the history of Y values here
    private List<float> wavePoints = new List<float>();

    // We track phase manually to prevent "popping" when changing frequency
    private float currentPhase = 0f;

    // Timer to handle the scrolling speed independent of frame rate
    private float updateTimer = 0f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;

        // Initialize the list with zeros so the line doesn't start empty
        for (int i = 0; i < resolution; i++)
        {
            wavePoints.Add(0f);
        }
    }

    void Update()
    {
        UpdateVisuals();
        UpdateWaveLogic();
        DrawWave();
    }

    void UpdateVisuals()
    {
        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;
        lineRenderer.startColor = waveColor;
        lineRenderer.endColor = waveColor;
    }

    void UpdateWaveLogic()
    {
        // 1. Calculate how much time represents one "step" in our resolution
        // Speed = Units per second. 
        // Resolution Spacing = Length / Resolution.
        // Time per step = Spacing / Speed.
        float timePerStep = (waveLength / resolution) / Mathf.Max(speed, 0.001f);

        updateTimer += Time.deltaTime;

        // 2. While enough time has passed to generate a new point...
        while (updateTimer >= timePerStep)
        {
            updateTimer -= timePerStep;

            // Increment phase based on current frequency
            // 2PI * Frequency * timeStep
            currentPhase += 2f * Mathf.PI * frequency * timePerStep;

            // Keep phase within 0-2PI to prevent float overflow over long periods
            currentPhase %= 2f * Mathf.PI;

            // Calculate the new value (The "Head" of the wave)
            float newY = (Mathf.Sin(currentPhase) * amplitude) + verticalShift;

            // Add new point to the end (Right side)
            wavePoints.Add(newY);

            // Remove old point from the start (Left side) to keep resolution constant
            if (wavePoints.Count > resolution)
            {
                wavePoints.RemoveAt(0);
            }
        }
    }

    void DrawWave()
    {
        lineRenderer.positionCount = wavePoints.Count;
        Vector3[] positions = new Vector3[wavePoints.Count];

        for (int i = 0; i < wavePoints.Count; i++)
        {
            // Calculate X: Evenly spaced across the waveLength
            float x = ((float)i / (wavePoints.Count - 1)) * waveLength;

            // Retrieve Y from history
            float rawY = wavePoints[i];

            // Apply the Bounding Box (Clamping)
            // This flattens the wave if it hits the top or bottom
            float clampedY = Mathf.Clamp(rawY, yMin, yMax);

            positions[i] = new Vector3(x, clampedY, 0f);
        }

        lineRenderer.SetPositions(positions);
    }

    public void ChangeFrequency(float direction)
    {
        frequency -= direction * 0.1f;
        frequency = Mathf.Clamp(frequency, minFrequency, maxFrequency);
    }

    public void ChangeAmplitude(float direction)
    {
        amplitude -= direction; 
        amplitude = Mathf.Clamp(amplitude, minAmplitude, maxAmplitude);
    }

    // Add inside OscilloscopeWave.cs

    public void NudgeAmplitude(float amount)
    {
        amplitude += amount;
        amplitude = Mathf.Clamp(amplitude, minAmplitude, maxAmplitude);
    }

    public void NudgeFrequency(float amount)
    {
        frequency += amount;
        frequency = Mathf.Clamp(frequency, minFrequency, maxFrequency);
    }

    // Helper to visualize the bounds in Scene view
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position + new Vector3(waveLength / 2f, 0, 0);

        // Draw the top and bottom limits
        Vector3 topStart = transform.position + new Vector3(0, yMax, 0);
        Vector3 topEnd = transform.position + new Vector3(waveLength, yMax, 0);
        Vector3 botStart = transform.position + new Vector3(0, yMin, 0);
        Vector3 botEnd = transform.position + new Vector3(waveLength, yMin, 0);

        Gizmos.DrawLine(topStart, topEnd);
        Gizmos.DrawLine(botStart, botEnd);
    }
}