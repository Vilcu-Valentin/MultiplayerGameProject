using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitFlapDisplay : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private int numberOfUnits = 10;

    [Header("Layout")]
    [SerializeField] private float spacing = 0.5f;
    [SerializeField] private float unitScale = 1.0f;

    [Header("Animation Settings")]
    [SerializeField] private int minCycles = 3;
    [SerializeField] private int maxCycles = 6;
    [SerializeField] private float cascadeDelay = 0.05f;

    private const string VALID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -.:";

    private List<SplitFlapUnit> _units = new List<SplitFlapUnit>();

    // Track the COROUTINE running on each specific unit
    private Coroutine[] _activeUnitCoroutines;

    // Track what the unit is trying to become (if animating) or is currently (if idle)
    private char[] _intendedState;

    // Track the main cascading loop
    private Coroutine _cascadeCoroutine;

    private void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        _units.Clear();

        _activeUnitCoroutines = new Coroutine[numberOfUnits];
        _intendedState = new char[numberOfUnits];

        for (int i = 0; i < numberOfUnits; i++)
        {
            GameObject obj = Instantiate(unitPrefab, transform);
            float xPos = i * spacing;
            obj.transform.localPosition = new Vector3(xPos, 0, 0);
            obj.transform.localScale = Vector3.one * unitScale;

            SplitFlapUnit unit = obj.GetComponent<SplitFlapUnit>();
            if (unit != null)
            {
                // Initialize visuals and state to Empty
                unit.UpdateAllText(' ');
                _intendedState[i] = ' ';
                _units.Add(unit);
            }
        }
    }

    public void SetText(string message)
    {
        if (message == null) message = "";
        message = message.ToUpper();

        if (message.Length < numberOfUnits) message = message.PadRight(numberOfUnits);
        else if (message.Length > numberOfUnits) message = message.Substring(0, numberOfUnits);

        // 1. Stop the MAIN cascade loop.
        // This prevents units further down the line from starting to animate to the OLD text.
        if (_cascadeCoroutine != null) StopCoroutine(_cascadeCoroutine);

        // 2. Start a new cascade loop
        _cascadeCoroutine = StartCoroutine(AnimateBoardRoutine(message));
    }

    private IEnumerator AnimateBoardRoutine(string message)
    {
        for (int i = 0; i < _units.Count; i++)
        {
            char targetChar = message[i];

            // Check if this unit actually NEEDS to change.
            // If the unit is already assigned this character (either it's finished showing it,
            // OR it is currently animating towards it), we skip it.
            // This prevents "jitter" if you type "HELLO" and then "HELLO" again quickly.
            if (_intendedState[i] == targetChar) continue;

            // Update our "Intended" state immediately.
            // This marks the unit as "Busy handling this character".
            _intendedState[i] = targetChar;

            // If an animation is currently running on THIS specific unit, stop it.
            // This cleans up the "Gibberish" from the interrupted animation.
            if (_activeUnitCoroutines[i] != null)
            {
                StopCoroutine(_activeUnitCoroutines[i]);
            }

            // Start the new animation and track it
            SplitFlapUnit unit = _units[i];
            _activeUnitCoroutines[i] = StartCoroutine(CycleUnitRoutine(unit, targetChar, i));

            if (cascadeDelay > 0) yield return new WaitForSeconds(cascadeDelay);
        }
    }

    private IEnumerator CycleUnitRoutine(SplitFlapUnit unit, char finalChar, int index)
    {
        int cycles = Random.Range(minCycles, maxCycles);
        float waitTime = unit.flipDuration + 0.02f;

        for (int c = 0; c < cycles; c++)
        {
            char randomChar = VALID_CHARS[Random.Range(0, VALID_CHARS.Length)];
            unit.FlipTo(randomChar);
            yield return new WaitForSeconds(waitTime);
        }

        // Set final character
        unit.FlipTo(finalChar);

        // Mark this coroutine as finished in our tracker
        _activeUnitCoroutines[index] = null;
    }
}