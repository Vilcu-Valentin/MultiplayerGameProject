using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SplitFlapUnit : MonoBehaviour
{
    [Header("Settings")]
    public float flipDuration = 0.2f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("References (Static)")]
    [SerializeField] private TMP_Text topStaticText;
    [SerializeField] private TMP_Text bottomStaticText;

    [Header("References (Flipper)")]
    [SerializeField] private Transform flipperPivot; // The parent that rotates
    [SerializeField] private GameObject flipperContent; // To hide flipper when idle
    [SerializeField] private TMP_Text flipperFrontText;
    [SerializeField] private TMP_Text flipperBackText;

    private char _currentChar = ' ';

    private void Start()
    {
        // Initialization: Hide the moving flipper, set initial state
        flipperContent.SetActive(false);
        UpdateAllText(_currentChar);
    }

    // Call this from your game manager
    public void FlipTo(char targetChar)
    {
        if (_currentChar == targetChar) return;

        // If we are already flipping, force finish or queue (simple version: stop and restart)
        StopAllCoroutines();
        StartCoroutine(FlipRoutine(targetChar));
    }

    private IEnumerator FlipRoutine(char targetChar)
    {
        // 1. SETUP THE TEXTS
        // Bottom Static: Stays as the OLD character (until the flap falls covering it)
        bottomStaticText.text = _currentChar.ToString();

        // Top Static: Shows the NEW character (revealed when flap falls)
        topStaticText.text = targetChar.ToString();

        // Flipper Front: The face falling down (Old Character Top)
        flipperFrontText.text = _currentChar.ToString();

        // Flipper Back: The face coming down (New Character Bottom)
        flipperBackText.text = targetChar.ToString();

        // 2. PREPARE ANIMATION
        flipperContent.SetActive(true);
        flipperPivot.localRotation = Quaternion.Euler(0, 0, 0);

        float timer = 0f;

        // 3. ANIMATE
        while (timer < flipDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / flipDuration;

            // Evaluate curve for smooth "mechanical" feel
            float angle = flipCurve.Evaluate(progress) * -180f; // Rotate from 0 to 180

            flipperPivot.localRotation = Quaternion.Euler(angle, 0, 0);

            yield return null;
        }

        // 4. CLEANUP
        // Snap to final state
        flipperPivot.localRotation = Quaternion.Euler(0, 0, 0); // Reset rotation
        flipperContent.SetActive(false); // Hide flipper

        // Update the static top to match the new reality
        bottomStaticText.text = targetChar.ToString();

        _currentChar = targetChar;
    }

    // Helper to force set text without animation
    public void UpdateAllText(char c)
    {
        _currentChar = c;
        topStaticText.text = c.ToString();
        bottomStaticText.text = c.ToString();
        flipperFrontText.text = c.ToString();
        flipperBackText.text = c.ToString();
    }
}