using System.Collections;
using TMPro;
using UnityEngine;

public class SplitFlapUnit : MonoBehaviour
{
    [Header("Settings")]
    public float flipDuration = 0.2f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References (Static)")]
    [SerializeField] private TMP_Text topStaticText;
    [SerializeField] private TMP_Text bottomStaticText;

    [Header("References (Flipper)")]
    [SerializeField] private Transform flipperPivot;
    [SerializeField] private GameObject flipperContent;
    [SerializeField] private TMP_Text flipperFrontText;
    [SerializeField] private TMP_Text flipperBackText;

    private char _currentChar = ' ';

    private void Start()
    {
        flipperContent.SetActive(false);
        UpdateAllText(_currentChar);
    }

    // Safety: Ensure nothing hangs if the object is disabled
    private void OnDisable()
    {
        StopAllCoroutines();
        ResetVisualState();
    }

    public void FlipTo(char targetChar)
    {
        // 1. Stop any existing animation immediately
        StopAllCoroutines();

        // 2. IMPORTANT: Force finish the previous state visually
        // If we interrupted a flip, the visuals might be messy. Reset them.
        ResetVisualState();

        // 3. If we are just resetting to the same char, stop here
        if (_currentChar == targetChar) return;

        // 4. Start the new flip
        StartCoroutine(FlipRoutine(targetChar));
    }

    private void ResetVisualState()
    {
        // Snap rotation back to 0
        flipperPivot.localRotation = Quaternion.identity;
        // Hide the flipper
        flipperContent.SetActive(false);
        // Ensure the static text matches the logical state
        UpdateAllText(_currentChar);
    }

    private IEnumerator FlipRoutine(char targetChar)
    {
        // --- SETUP ---
        // Bottom Static: Shows the OLD character
        bottomStaticText.text = _currentChar.ToString();
        // Top Static: Shows the NEW character immediately (revealed behind the flap)
        topStaticText.text = targetChar.ToString();

        // Flipper Front: The face falling down (Old Character Top)
        flipperFrontText.text = _currentChar.ToString();
        // Flipper Back: The face coming down (New Character Bottom)
        flipperBackText.text = targetChar.ToString();

        flipperContent.SetActive(true);
        flipperPivot.localRotation = Quaternion.Euler(0, 0, 0);

        // --- ANIMATION ---
        float timer = 0f;
        while (timer < flipDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / flipDuration;
            float angle = flipCurve.Evaluate(progress) * -180f;
            flipperPivot.localRotation = Quaternion.Euler(angle, 0, 0);
            yield return null;
        }

        // --- FINISH ---
        // Commit the change
        _currentChar = targetChar;
        ResetVisualState();
    }

    public void UpdateAllText(char c)
    {
        _currentChar = c;
        if (topStaticText) topStaticText.text = c.ToString();
        if (bottomStaticText) bottomStaticText.text = c.ToString();
        if (flipperFrontText) flipperFrontText.text = c.ToString();
        if (flipperBackText) flipperBackText.text = c.ToString();
    }
}