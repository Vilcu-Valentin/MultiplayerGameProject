using UnityEngine;

public class CoilController : MonoBehaviour
{
    // Drag your MeshRenderer here in the inspector, 
    // or let Start() find it automatically.
    public Renderer coilRenderer;

    // Range creates a slider in the editor for testing
    [Range(0f, 1f)]
    public float currentFillValue = 0f;

    // Use the EXACT Reference name you set in Shader Graph
    private string fillProperty = "_FillAmount";

    void Start()
    {
        // Get the renderer if not manually assigned
        if (coilRenderer == null)
            coilRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // Update the shader value every frame
        // Note: accessing .material creates a unique instance of the material
        coilRenderer.material.SetFloat(fillProperty, currentFillValue);
    }

    // Call this function from other scripts to set the fill level
    public void SetFill(float value)
    {
        currentFillValue = Mathf.Clamp01(value);
        coilRenderer.material.SetFloat(fillProperty, currentFillValue);
    }
}