using System.Collections;
using System.Text; // Required for Encoding
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest
using TMPro; // Use TextMeshPro for modern UI

/// <summary>
/// Manages authentication with the game server (registration and login).
/// </summary>
public class AuthManager : MonoBehaviour
{
    [Header("Server Settings")]
    [Tooltip("The full URL to your server's register endpoint")]
    [SerializeField]
    private string registerUrl = "http://localhost:5000/api/Auth/register";

    [Header("Scene References")]
    [Tooltip("The input field for the username")]
    [SerializeField]
    private TMP_InputField usernameInput;

    [Tooltip("The input field for the password")]
    [SerializeField]
    private TMP_InputField passwordInput;

    [Tooltip("The text element to display success or error messages")]
    [SerializeField]
    private TMP_Text messageText;

    /// <summary>
    /// A simple class to hold our registration data for JSON serialization.
    /// The variable names MUST match the DTO on the server (username, password).
    /// </summary>
    [System.Serializable]
    private class RegisterPayload
    {
        public string username;
        public string password;
    }


    public void OnRegisterButtonClick()
    {
        // Clear any previous messages
        messageText.text = "";

        // Basic validation
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            messageText.text = "Username and password cannot be empty.";
            messageText.color = Color.red;
            return;
        }

        // Start the web request as a Coroutine
        StartCoroutine(RegisterCoroutine());
    }

    /// <summary>
    /// Coroutine to handle the asynchronous web request for registration.
    /// </summary>
    private IEnumerator RegisterCoroutine()
    {
        messageText.text = "Registering...";
        messageText.color = Color.white; // Use a neutral color

        RegisterPayload payload = new RegisterPayload
        {
            username = usernameInput.text,
            password = passwordInput.text
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        // Convert the JSON string to a byte array
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        // Create and configure the UnityWebRequest
        // "using" block ensures the web request is disposed of properly
        using (UnityWebRequest uwr = new UnityWebRequest(registerUrl, "POST"))
        {
            // Set the upload handler to send our JSON data
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            // Set the download handler to receive the server's response
            uwr.downloadHandler = new DownloadHandlerBuffer();

            // Set the Content-Type header (CRITICAL for APIs expecting JSON)
            uwr.SetRequestHeader("Content-Type", "application/json");

            // Send the request and wait for it to complete
            yield return uwr.SendWebRequest();

            // Check the response
            if (uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                // Server responded with an error code (4xx, 5xx)
                if (uwr.responseCode == 400)
                {
                    messageText.text = "Username taken";
                    messageText.color = Color.red;
                    Debug.LogWarning("Registration failed: Username likely taken.");
                }
                else
                {
                    // Other server error
                    messageText.text = "Error: " + uwr.responseCode;
                    messageText.color = Color.red;
                    Debug.LogError("Registration Error: " + uwr.responseCode + " - " + uwr.downloadHandler.text);
                }
            }
            else if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                // Network connection error (e.g., server is down)
                messageText.text = "Connection Error. Is the server running?";
                messageText.color = Color.red;
                Debug.LogError("Connection Error: " + uwr.error);
            }
            else
            {
                // Success! (HTTP 2xx)
                if (uwr.responseCode == 201)
                {
                    // As requested: "Success!"
                    messageText.text = "Success! You can now log in.";
                    messageText.color = Color.green;
                    Debug.Log("Registration Successful!");
                }
                else
                {
                    // Unexpected but successful response code
                    messageText.text = "Unexpected response: " + uwr.responseCode;
                    messageText.color = Color.yellow;
                    Debug.LogWarning("Unexpected success response code: " + uwr.responseCode);
                }
            }
        }
    }
}