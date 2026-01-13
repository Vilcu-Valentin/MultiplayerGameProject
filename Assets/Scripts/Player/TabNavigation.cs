using System.Collections.Generic;
using UnityEngine;
using TMPro; // Needed for TextMeshPro
using UnityEngine.EventSystems;

public class TabNavigation : MonoBehaviour
{
    [Header("Drag Input Fields here in the desired order")]
    [SerializeField] private List<TMP_InputField> inputFields;

    private void Update()
    {
        // Only run if the Tab key is pressed
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inputFields.Count == 0) return;

            // Check if Shift is held down for reverse navigation
            bool isShiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Find which input field is currently selected
            int currentIndex = -1;
            GameObject currentObj = EventSystem.current.currentSelectedGameObject;

            // Check if the currently selected object is in our list
            for (int i = 0; i < inputFields.Count; i++)
            {
                if (inputFields[i].gameObject == currentObj)
                {
                    currentIndex = i;
                    break;
                }
            }

            // Calculate the next index
            int nextIndex = 0;

            if (currentIndex != -1)
            {
                if (isShiftPressed)
                {
                    // Reverse logic: Add count to avoid negative numbers in modulo
                    nextIndex = (currentIndex - 1 + inputFields.Count) % inputFields.Count;
                }
                else
                {
                    // Forward logic
                    nextIndex = (currentIndex + 1) % inputFields.Count;
                }
            }
            // If nothing is selected (currentIndex == -1), nextIndex stays 0, selecting the first field.

            // select and activate the new field
            SelectField(inputFields[nextIndex]);
        }
    }

    private void SelectField(TMP_InputField field)
    {
        if (field != null)
        {
            field.Select();
            field.ActivateInputField(); // Vital: pops up the keyboard/cursor immediately
        }
    }
}