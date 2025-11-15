using System;
using System.Collections;
using UnityEngine;

public class PosePresenter : MonoBehaviour
{
    public static PosePresenter Instance { get; private set; }

    private PlayerMovement playerMovement;
    private Player3DMovement player3DMovement;
    private PlayerAnimation playerAnim;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-find player components
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            playerMovement = playerGO.GetComponent<PlayerMovement>();
            player3DMovement = playerGO.GetComponent<Player3DMovement>();
            playerAnim = playerGO.GetComponent<PlayerAnimation>();
        }
        else
        {
            Debug.LogError("PosePresenter: Player with tag 'Player' not found!");
        }
    }

    public void EnterPose(Vector3 targetPos, float targetYRotation, PoseConfig config)
    {
        if (config == null || playerAnim == null) return;

        StartCoroutine(PoseTransitionSequence(targetPos, targetYRotation, config, true)); // true = enter
    }

    public void ExitPose(PoseConfig config)
    {
        if (config == null || playerAnim == null) return;

        playerAnim.InstantExitPose(config);
    }

    // Convenience overload - uses tracked currentConfig (no param needed)
    public void ExitPose()
    {
        if (playerAnim == null || !playerAnim.isInPose) return;
        playerAnim.InstantExitPose(playerAnim.currentConfig); 
    }

    private IEnumerator PoseTransitionSequence(Vector3 targetPos, float targetYRotation, PoseConfig config, bool isEnter)
    {
        if (!isEnter)
        {
            // For exit, just invoke instant (already handled above)
            yield break;
        }
        // Pre-transition event (e.g., sound)
        // onPoseStart?.Invoke(config.poseID, isEnter);

        // Black screen
        yield return ScreenFader.Instance.FadeToBlack(duration: 0f, frameWait: 0);

        // All work under black: Reposition, track, trigger anim, disable
        PerformEnterPose(targetPos, targetYRotation, config);

        // Hold black for config duration during/after transition (realtime to ignore timeScale)
        yield return new WaitForSecondsRealtime(config.blackHoldDuration);

        // Fade out
        yield return ScreenFader.Instance.FadeFromBlack(duration: 0f);
    }

    private void PerformEnterPose(Vector3 targetPos, float targetYRotation, PoseConfig config)
    {
        if (playerMovement == null || playerAnim == null) return;

        // Preserve X/Z rotation, apply Y from target
        Vector3 currentEuler = playerMovement.transform.eulerAngles;

        // Reposition
        playerMovement.transform.position = targetPos;
        playerMovement.transform.eulerAngles = new Vector3(currentEuler.x, targetYRotation, currentEuler.z);

        // Set tracking *before* anim trigger (for immediate state)
        playerAnim.SetCurrentPose(config.poseID, config);
        playerAnim.SetCurrentExitTrigger(config.exitTrigger);

        // Trigger anim, disable movement
        playerAnim.TriggerEnterPose(config.enterTrigger, config.useRootMotion);
        DisableMovement();
    }

    private void DisableMovement()
    {
        if (playerMovement != null) playerMovement.controlsEnabled = false;
        if (player3DMovement != null) player3DMovement.controlsEnabled = false;
    }
}
