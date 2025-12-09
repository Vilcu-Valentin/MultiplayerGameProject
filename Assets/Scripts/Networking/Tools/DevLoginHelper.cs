using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// AUTO-LOGIN FOR DEVELOPMENT.
/// This script only runs inside the Unity Editor.
/// It detects if we skipped the Login Scene and automatically logs in a test user
/// so we can test the Workstation functionality immediately.
/// </summary>
public class DevLoginHelper : MonoBehaviour
{
#if UNITY_EDITOR // This entire class is stripped out of the final game build

    [Header("Dev Credentials")]
    [SerializeField] private string devUsername = "dev_player"; // Make sure this user exists in DB!
    [SerializeField] private string devPassword = "password123";
    [SerializeField] private string loginUrl = "http://localhost:5000/api/Auth/login";

    [Header("Prefabs (Optional)")]
    [Tooltip("Drag your MainThreadDispatcher prefab here if it's not in the scene")]
    [SerializeField] private GameObject dispatcherPrefab;
    [Tooltip("Drag your SignalRManager prefab here if it's not in the scene")]
    [SerializeField] private GameObject signalRManagerPrefab;

    private void Awake()
    {
        // 1. Check if a GameSession already exists.
        if (GameSession.Instance != null)
        {
            // We likely came from the Login Menu correctly, so we don't need this helper.
            Debug.Log("[DevHelper] GameSession found. Disabling DevHelper.");
            gameObject.SetActive(false);
            return;
        }

        Debug.LogWarning("[DevHelper] No GameSession found! Starting Dev Auto-Login...");
        SetupDependencies();
        StartCoroutine(AutoLogin());
    }

    private void SetupDependencies()
    {
        // 1. Create GameSession
        GameObject sessionObj = new GameObject("GameSession");
        sessionObj.AddComponent<GameSession>();
        // Note: GameSession.Awake() sets the Instance singleton.

        // 2. Ensure Dispatcher exists
        if (MainThreadDispatcher.Instance == null)
        {
            if (dispatcherPrefab != null) Instantiate(dispatcherPrefab);
            else new GameObject("MainThreadDispatcher").AddComponent<MainThreadDispatcher>();
        }

        // 3. Ensure SignalR Manager exists
        if (SignalRManager.Instance == null)
        {
            if (signalRManagerPrefab != null) Instantiate(signalRManagerPrefab);
            else new GameObject("SignalRManager").AddComponent<SignalRManager>();
        }
    }

    [System.Serializable]
    class DevLoginPayload { public string username; public string password; }

    [System.Serializable]
    class DevLoginResponse { public string token; }

    private IEnumerator AutoLogin()
    {
        var payload = new DevLoginPayload { username = devUsername, password = devPassword };
        byte[] jsonToSend = new UTF8Encoding().GetBytes(JsonUtility.ToJson(payload));

        using (UnityWebRequest uwr = new UnityWebRequest(loginUrl, "POST"))
        {
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<DevLoginResponse>(uwr.downloadHandler.text);

                // Inject the token into the Session
                if (GameSession.Instance != null)
                {
                    GameSession.Instance.AuthToken = response.token;
                    GameSession.Instance.Username = devUsername;
                    Debug.Log($"<color=cyan>[DevHelper] Auto-Login Successful! Token injected.</color>");
                }
            }
            else
            {
                Debug.LogError($"[DevHelper] Auto-Login Failed: {uwr.responseCode} - {uwr.downloadHandler.text}. Make sure the user '{devUsername}' exists in your DB!");
            }
        }
    }
#endif
}