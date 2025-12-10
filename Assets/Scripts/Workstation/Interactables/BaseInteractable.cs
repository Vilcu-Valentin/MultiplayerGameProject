using UnityEngine;
using UnityEngine.Events;

// Inherit from this for specific controls
public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    [Header("Base Settings")]
    [SerializeField] protected AudioClip[] audioClips;

    // We expose a generic event for external systems (SoundManager, Analytics, etc.)
    public UnityEvent OnHoverStart;
    public UnityEvent OnHoverEnd;

    protected bool _isInteracting = false;
    protected Camera _mainCamera;

    protected virtual void Start()
    {
        _mainCamera = Camera.main;
    }

    protected virtual void Update() { }

    public virtual void OnHoverEnter()
    {
        Debug.Log("Focused");
        var rend = gameObject.GetComponent<Renderer>();
        if (rend != null)
                SelectionOutlineManager.Instance.Add(rend);
        OnHoverStart?.Invoke();
    }

    public virtual void OnHoverExit()
    {
        Debug.Log("Unfocused");
        var rend = gameObject.GetComponent<Renderer>();
        if (rend != null)
                SelectionOutlineManager.Instance.Remove(rend);
        OnHoverEnd?.Invoke();
    }

    public virtual void OnInteractStart()
    {
        _isInteracting = true;
    }

    public virtual void OnInteractEnd()
    {
        _isInteracting = false;
    }

    // --- Helper for Audio --- //
    protected void PlaySound(int index = 0)
    {
        // Decoupled: We don't hardcode SoundManager.Instance here if we can avoid it,
        /*if (audioClips.Length > index && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundFX(audioClips[index], transform);
        } */
    }
}