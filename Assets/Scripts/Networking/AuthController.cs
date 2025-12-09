using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement; // Required for changing scenes
using TMPro;

public class AuthManager : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField]
    private string baseUrl = "http://localhost:5000/";

    [SerializeField]
    private string registerEndpoint = "api/Auth/register";

    [SerializeField]
    private string loginEndpoint = "api/Auth/login";

    [Header("Scene References")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_Text messageText;

    [System.Serializable]
    private class RegisterPayload
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string token;
    }

    // --- REGISTER LOGIC ---

    public void OnRegisterButtonClick()
    {
        messageText.text = "";
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            messageText.text = "Username and password cannot be empty.";
            messageText.color = Color.red;
            return;
        }
        StartCoroutine(RegisterCoroutine());
    }

    private IEnumerator RegisterCoroutine()
    {
        messageText.text = "Registering...";
        messageText.color = Color.white;

        RegisterPayload payload = new RegisterPayload { username = usernameInput.text, password = passwordInput.text };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        // Combine base URL with endpoint
        string fullUrl = baseUrl + registerEndpoint;

        using (UnityWebRequest uwr = new UnityWebRequest(fullUrl, "POST"))
        {
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                if (uwr.responseCode == 400) { messageText.text = "Username taken"; messageText.color = Color.red; }
                else { messageText.text = "Error: " + uwr.responseCode; messageText.color = Color.red; }
            }
            else if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                messageText.text = "Connection Error."; messageText.color = Color.red;
            }
            else if (uwr.responseCode == 201)
            {
                messageText.text = "Success! You can now log in.";
                messageText.color = Color.green;
            }
        }
    }

    // --- LOGIN LOGIC ---

    public void OnLoginButtonClick()
    {
        messageText.text = "";
        if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
        {
            messageText.text = "Username and password cannot be empty.";
            messageText.color = Color.red;
            return;
        }
        StartCoroutine(LoginCoroutine());
    }

    private IEnumerator LoginCoroutine()
    {
        messageText.text = "Logging in...";
        messageText.color = Color.white;

        RegisterPayload payload = new RegisterPayload { username = usernameInput.text, password = passwordInput.text };
        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

        string fullUrl = baseUrl + loginEndpoint;

        using (UnityWebRequest uwr = new UnityWebRequest(fullUrl, "POST"))
        {
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                if (uwr.responseCode == 401)
                {
                    messageText.text = "Invalid username or password";
                    messageText.color = Color.red;
                }
                else
                {
                    messageText.text = "Server Error: " + uwr.responseCode;
                    messageText.color = Color.red;
                }
            }
            else if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                messageText.text = "Connection Error.";
                messageText.color = Color.red;
            }
            else if (uwr.responseCode == 200)
            {
                // 1. Parse Response
                string jsonResponse = uwr.downloadHandler.text;
                LoginResponse responseData = JsonUtility.FromJson<LoginResponse>(jsonResponse);

                // 2. Save to GameSession
                if (GameSession.Instance != null)
                {
                    GameSession.Instance.AuthToken = responseData.token;
                    GameSession.Instance.Username = usernameInput.text;

                    messageText.text = "Success! Loading...";
                    messageText.color = Color.green;

                    Debug.Log($"Token saved: {responseData.token.Substring(0, 10)}...");

                    // 3. Change Scene
                    // Make sure "WorkstationScene" is added in File -> Build Settings
                    SceneManager.LoadScene("WorkstationScene");
                }
                else
                {
                    Debug.LogError("GameSession instance not found! Did you create the GameSession object in the scene?");
                    messageText.text = "Internal Error: Session Missing";
                    messageText.color = Color.red;
                }
            }
        }
    }
}