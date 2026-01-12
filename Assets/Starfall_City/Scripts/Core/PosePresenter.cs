#nullable enable
using System.Collections;
using Cysharp.Threading.Tasks;
using Interaction;
using Movement;
using UnityEngine;

namespace core
{
    public class PosePresenter : MonoBehaviour
    {
        public static PosePresenter Instance { get; private set; }

        private PlayerMovement _playerMovement;
        private Player3DMovement _player3DMovement;
        private PlayerAnimation _playerAnim;

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
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                _playerMovement = playerGO.GetComponent<PlayerMovement>();
                _player3DMovement = playerGO.GetComponent<Player3DMovement>();
                _playerAnim = playerGO.GetComponent<PlayerAnimation>();
            }
            else
            {
                Debug.LogError("PosePresenter: Player with tag 'Player' not found!");
            }
        }

        public void EnterPose(Vector3 targetPos, float targetYRotation, PoseConfig config)
        {
            Debug.Log($"EnterPose called: Pos={targetPos}, YRot={targetYRotation}, Config={config.name}");
            if (config == null || _playerAnim == null)
            {
                Debug.LogError("EnterPose: Config or PlayerAnim null!");
                return;
            }

            StartCoroutine(PoseTransitionSequence(targetPos, targetYRotation, config, true)); // true = enter
        }

        public void ExitPose(PoseConfig? config = null, string? skipIfPoseID = null)
        {
            Debug.Log($"ExitPose called! Args: config={config.name ?? "null"}, skipIfPoseID={skipIfPoseID ?? "null"}");

            if (_playerAnim == null)
            {
                Debug.LogError("ExitPose: playerAnim null! (Re-find? Scene reload?)");
                return;
            }
            if (!_playerAnim.IsInPose)
            {
                Debug.LogWarning($"ExitPose: !isInPose (current: {_playerAnim.IsInPose}) - skipping.");
                return;
            }
            if (!string.IsNullOrEmpty(skipIfPoseID) && skipIfPoseID == _playerAnim.CurrentPoseID)
            {
                Debug.Log($"Skipping exit for pose: {_playerAnim.CurrentPoseID}");
                return;
            }

            Debug.Log("ExitPose: Proceeding to InstantExitPose.");

            if (_playerAnim.CurrentConfig != null)
            {
                var effectiveConfig = _playerAnim.CurrentConfig;
                _playerAnim.InstantExitPose(effectiveConfig);
            }
            else
            {
                Debug.LogError("ExitPose: playerAnim config is null!");
            }
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
            yield return ScreenFader.Instance.FadeToBlackAsync(duration: 0f, frameWait: 0).ToCoroutine();

            // All work under black: Reposition, track, trigger anim, disable
            PerformEnterPose(targetPos, targetYRotation, config);

            // Hold black for config duration during/after transition (realtime to ignore timeScale)
            yield return new WaitForSecondsRealtime(config.blackHoldDuration);

            // Fade out
            yield return ScreenFader.Instance.FadeFromBlackAsync(duration: 0f).ToCoroutine();
        }

        private void PerformEnterPose(Vector3 targetPos, float targetYRotation, PoseConfig config)
        {
            if (_playerMovement == null || _playerAnim == null) return;

            // Preserve X/Z rotation, apply Y from target
            var currentEuler = _playerMovement.transform.eulerAngles;

            // Reposition
            _playerMovement.transform.position = targetPos;
            _playerMovement.transform.eulerAngles = new Vector3(currentEuler.x, targetYRotation, currentEuler.z);

            // Set tracking *before* anim trigger (for immediate state)
            _playerAnim.SetCurrentPose(config.poseID, config);
            _playerAnim.SetCurrentExitTrigger(config.exitTrigger);

            // Trigger anim, disable movement
            _playerAnim.TriggerEnterPose(config.enterTrigger, config.useRootMotion);
            DisableMovement();
        }

        private void DisableMovement()
        {
            if (_playerMovement != null) _playerMovement.controlsEnabled = false;
            if (_player3DMovement != null) _player3DMovement.controlsEnabled = false;
        }
    }
}
