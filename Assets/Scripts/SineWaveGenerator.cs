using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SineWaveGenerator : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveLength = 10f;    // Total length of the line
    public float amplitude = 1f;      // Height of the wave
    public float frequency = 1f;      // How many crests within the length
    public float speed = 2f;          // Movement speed (animation)
    public float xOffset = 0f;        // Manual offset for scrolling

    [Header("Quality")]
    [Tooltip("Higher numbers make the curve smoother but cost more performance.")]
    public int resolution = 50;       // Number of points in the line

    [Header("Appearance")]
    public float thickness = 0.1f;
    public Color waveColor = Color.cyan;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // Ensure the line moves with the object rather than sticking to world coordinates
        lineRenderer.useWorldSpace = false;
    }

    void Update()
    {
        UpdateVisuals();
        DrawWave();
    }

    void UpdateVisuals()
    {
        // Update thickness
        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;

        // Update color
        // Note: The Material used must support Vertex Colors (e.g., Sprites/Default)
        lineRenderer.startColor = waveColor;
        lineRenderer.endColor = waveColor;
    }

    void DrawWave()
    {
        lineRenderer.positionCount = resolution;

        for (int i = 0; i < resolution; i++)
        {
            // Calculate progress (0 to 1) along the line
            float progress = (float)i / (resolution - 1);

            // Calculate X position based on total length
            float x = progress * waveLength;

            // Calculate Y position using the Sine function
            // Formula: y = Amplitude * Sin(Frequency * x + MovingOffset)
            float outputFrequency = frequency * 2f * Mathf.PI; // Convert to radians/cycle
            float movement = (Time.time * speed) + xOffset;

            // Note: We divide movement by waveLength to keep speed consistent regardless of length
            float y = amplitude * Mathf.Sin((progress * outputFrequency) + movement);

            // Set the point in local space (Z is 0 to keep it flat)
            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }
}