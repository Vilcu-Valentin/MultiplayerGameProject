using System.Collections.Generic;
using UnityEngine;

public class SelectionOutlineManager : MonoBehaviour
{
    public static SelectionOutlineManager Instance { get; private set; }

    // Renderers currently marked for outline
    private readonly List<Renderer> selectedRenderers = new();

    void Awake()
    {
        if (Instance && Instance != this) Destroy(this);
        else Instance = this;
    }

    public void Add(Renderer r)
    {
        if (r != null && !selectedRenderers.Contains(r))
            selectedRenderers.Add(r);
    }

    public void Remove(Renderer r)
    {
        if (r != null)
            selectedRenderers.Remove(r);
    }

    public void Clear()
    {
        selectedRenderers.Clear();
    }

    public IReadOnlyList<Renderer> GetSelectedRenderers() => selectedRenderers;
}
