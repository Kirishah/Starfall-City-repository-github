using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(1000)] // Run after transfers
public class CinemachineRebinder : MonoBehaviour
{
    [Tooltip("Tag of the object that Cinemachine should follow (usually Player)")]
    public string followTag = "Player";

    private void Start() => RebindAllCameras();

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
        // Small delay to ensure transfers are done
        Invoke(nameof(RebindAllCameras), 0.1f);

    private void RebindAllCameras()
    {
        var player = GameObject.FindGameObjectWithTag(followTag);
        if (player == null)
        {
            Debug.LogWarning($"CinemachineRebinder: No object with tag '{followTag}' found!");
            return;
        }

        var vCams = Object.FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
        foreach (var vcam in vCams)
        {
            if (vcam.Follow == null || vcam.Follow.gameObject.CompareTag(followTag))
            {
                vcam.Follow = player.transform;
                vcam.LookAt = player.transform; // Optional: if you use LookAt
                Debug.Log($"Rebound Cinemachine camera: {vcam.name} → {player.name}");
            }
        }
    }
}
