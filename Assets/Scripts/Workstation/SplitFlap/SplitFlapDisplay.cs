using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitFlapDisplay : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private int numberOfUnits = 10;

    [Header("Layout")]
    [SerializeField] private float spacing = 0.5f; // Distance between units
    [SerializeField] private float unitScale = 1.0f; // Scale of the units

    [Header("Animation Settings")]
    [SerializeField] private int minCycles = 3; // Minimum random flips before stopping
    [SerializeField] private int maxCycles = 6; // Maximum random flips
    [SerializeField] private float cascadeDelay = 0.05f; // Delay between the 1st and 2nd letter starting

    // The characters the board can cycle through for the "random" effect
    private const string VALID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -.:";

    private List<SplitFlapUnit> _units = new List<SplitFlapUnit>();

    private void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        // Clear existing children if any (mostly for editor safety)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _units.Clear();

        // Spawn Units
        for (int i = 0; i < numberOfUnits; i++)
        {
            GameObject obj = Instantiate(unitPrefab, transform);

            // Positioning (Horizontal)
            // Assumes the prefab is centered. We move them along local X.
            float xPos = i * spacing;
            obj.transform.localPosition = new Vector3(xPos, 0, 0);
            obj.transform.localScale = Vector3.one * unitScale;

            SplitFlapUnit unit = obj.GetComponent<SplitFlapUnit>();
            if (unit != null)
            {
                // Set initial text to empty space or a dash
                unit.UpdateAllText(' ');
                _units.Add(unit);
            }
        }
    }

    public void SetText(string message)
    {
        // 1. Sanitize and Pad the string
        message = message.ToUpper();

        // If message is shorter than board, pad with spaces
        if (message.Length < numberOfUnits)
        {
            message = message.PadRight(numberOfUnits);
        }
        // If message is longer, truncate
        else if (message.Length > numberOfUnits)
        {
            message = message.Substring(0, numberOfUnits);
        }

        // 2. Trigger the animations
        StartCoroutine(AnimateBoardRoutine(message));
    }

    private IEnumerator AnimateBoardRoutine(string message)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            char targetChar = message[i];
            SplitFlapUnit unit = _units[i];

            // Start the individual unit's cycle routine
            StartCoroutine(CycleUnitRoutine(unit, targetChar));

            // Small delay before starting the next neighbor (The Cascade Effect)
            if (cascadeDelay > 0)
            {
                yield return new WaitForSeconds(cascadeDelay);
            }
        }
    }

    private IEnumerator CycleUnitRoutine(SplitFlapUnit unit, char finalChar)
    {
        // Determine how many times this specific unit will "flicker"
        int cycles = Random.Range(minCycles, maxCycles);

        // We need to know how fast the unit flips to sync our commands
        float waitTime = unit.flipDuration; // Ensure flipDuration is PUBLIC in SplitFlapUnit

        for (int i = 0; i < cycles; i++)
        {
            // Pick a random character for the effect
            char randomChar = VALID_CHARS[Random.Range(0, VALID_CHARS.Length)];

            unit.FlipTo(randomChar);

            // Wait for the flip to finish before sending the next one
            yield return new WaitForSeconds(waitTime);
        }

        // Finally, land on the correct character
        unit.FlipTo(finalChar);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetText(testString);
        }
    }

    // Debug Testing
    [Header("Debug")]
    public string testString = "HELLO WORLD";
}