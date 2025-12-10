using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactLayer;

    // The object we are currently looking at (for outlines/tooltips)
    private IInteractable _hoveredInteractable;

    // The object we are currently holding/dragging (locked interaction)
    private IInteractable _lockedInteractable;

    void Update()
    {
        // If we are holding something, skip the raycast. 
        // We don't want to switch hover targets while dragging a knob.
        if (_lockedInteractable != null)
        {
            HandleLockedInput();
        }
        else
        {
            HandleRaycast();
            HandleInput();
        }
    }

    void HandleRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Raycast check
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            IInteractable hitInteractable = hit.collider.GetComponent<IInteractable>();

            // If we hit a NEW object
            if (_hoveredInteractable != hitInteractable)
            {
                // Unhighlight old
                _hoveredInteractable?.OnHoverExit();

                // Highlight new
                _hoveredInteractable = hitInteractable;
                _hoveredInteractable?.OnHoverEnter();
            }
        }
        else
        {
            // We hit nothing, clear the current hover
            if (_hoveredInteractable != null)
            {
                _hoveredInteractable.OnHoverExit();
                _hoveredInteractable = null;
            }
        }
    }

    // Handles looking for the initial click
    void HandleInput()
    {
        // We can't interact if we aren't hovering over anything
        if (_hoveredInteractable == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            // LOCK interaction to this object
            _lockedInteractable = _hoveredInteractable;
            _lockedInteractable.OnInteractStart();
        }
    }

    // Handles the release, regardless of where the mouse is pointing
    void HandleLockedInput()
    {
        if (Input.GetMouseButtonUp(0))
        {
            // Unlock and end interaction
            _lockedInteractable.OnInteractEnd();
            _lockedInteractable = null;

            // Optional: Immediately re-run raycast so we don't have a 1-frame gap in outlines
            HandleRaycast();
        }
    }
}