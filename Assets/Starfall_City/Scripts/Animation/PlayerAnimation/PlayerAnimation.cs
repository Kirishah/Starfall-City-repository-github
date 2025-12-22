#nullable enable

using QTE;
using System;
using System.Collections;
using UnityEngine;
using Interaction;

public class PlayerAnimation : MonoBehaviour
{
    private Animator _animator;
    private PlayerMovement _playerMovement;
    private Player3DMovement _player3DMovement;
    private CharacterController _controller;

    [Header("Anim params")]
    [SerializeField, Tooltip("Degrees per second to trigger turn")]
    private float _turnThreshold = 100f;
    [SerializeField, Tooltip("Prevent rapid successive turns")]
    private float _turnCooldown = 1.0f;
    [SerializeField] private float _speedThreshold = 0.1f;
    [SerializeField] private float _smoothTime = 0.1f;

    [Header("Pose State")]
    [SerializeField] private PoseConfig? _currentConfig;
    private Coroutine? _currentEnterCoroutine;
    private Coroutine? _currentExitCoroutine;
    private float _currentSpeed;

    // Turn animation variables
    private Vector3 _previousDesired;
    private float _lastTurnTime;

    // Public read-only access
    public bool IsInPose { get; private set; }
    public PoseConfig? CurrentConfig => _currentConfig;
    public string? CurrentPoseID { get; private set; }
    public string? CurrentExitTrigger { get; private set; }


    void Start()
    {
        _animator = GetComponent<Animator>();
        _playerMovement = GetComponent<PlayerMovement>();
        _player3DMovement = GetComponent<Player3DMovement>();
        _controller = GetComponent<CharacterController>();

        IsInPose = false;
        CurrentPoseID = null;
        CurrentExitTrigger = null;
        _currentConfig = null;

        // Initialize turn tracking
        _previousDesired = transform.forward;
        _lastTurnTime = -_turnCooldown; // Allow immediate turn
    }

    // Public setters for tracking (called from PosePresenter)
    public void SetCurrentPose(string? poseID, PoseConfig? config)
    {
        CurrentPoseID = poseID ?? throw new ArgumentNullException(nameof(poseID));
        _currentConfig = config ?? throw new ArgumentNullException(nameof(config));
        Debug.Log($"Entered pose: {CurrentPoseID} (using config: {config?.name ?? "null"})");
    }

    public void SetCurrentExitTrigger(string exitTrigger) => CurrentExitTrigger = exitTrigger ?? throw new ArgumentNullException(nameof(exitTrigger));

    private void OnAnimatorMove()
    {
        if (_animator.applyRootMotion && _controller != null && _player3DMovement.IsInTransitionAnimation)
        {
            // Apply position delta from root motion to CharacterController
            _controller.Move(_animator.deltaPosition);

            // Optional: Apply rotation if your get-up anim includes root rotation
            // transform.rotation = animator.deltaRotation * transform.rotation;
        }
    }

    void Update()
    {
        if (QTEGameManager.IsQTEActive) return;
        CheckMovement();

        if (IsInPose)
        {
            _animator.SetBool("isSitting", true);
            _animator.SetBool("is_Walking", false); // Override walking during sit
        }
        else
        {
            _animator.SetBool("isSitting", false);
        }

        CheckTurnAnimation();
    }

    void CheckMovement()
    {
        // Check if the player is moving
        Vector3 velocity = _playerMovement.GetVelocity();

        float targetSpeed = velocity.magnitude;
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, _smoothTime);
        bool isMoving = _currentSpeed > _speedThreshold;


        if (isMoving)
        {
            // If the player is moving, set the walking animation
            _animator.SetBool("is_Walking", true);
        }
        else
        {
            // If the player is not moving, set the standing animation
            _animator.SetBool("is_Walking", false);
        }
    }

    void CheckTurnAnimation()
    {
        // Get desired direction based on movement mode
        Vector3 desired = Vector3.zero;
        if (_playerMovement.player.enabled)
        {
            desired = _playerMovement.GetDesiredDirection();
        }
        else
        {
            desired = _player3DMovement.GetDesiredDirection();
        }

        // Only check for turns when moving and have a valid desired direction
        if (desired.magnitude > 0.01f && _animator.GetBool("is_Walking"))
        {
            // Calculate turn angle between previous desired and current desired
            float turnAngle = Vector3.SignedAngle(_previousDesired, desired, Vector3.up);
            float turnRate = Mathf.Abs(turnAngle) / Time.deltaTime; // Degrees per second

            // Check if we should trigger a 180 turn
            if (turnRate > _turnThreshold && Mathf.Abs(turnAngle) > 90f &&
                Time.time - _lastTurnTime > _turnCooldown)
            {
                _animator.SetTrigger("Turn180");
                _lastTurnTime = Time.time;
            }

            // Update previous desired only if valid
            _previousDesired = desired;
        }
    }

    // Generalized enter 
    public void TriggerEnterPose(string enterTrigger, bool enableRootMotion = false)
    {
        if (IsInPose)
        {
            Debug.LogWarning($"Already in pose '{CurrentPoseID}'. Skipping enter.");
            return;
        }

        if (_currentEnterCoroutine != null) StopCoroutine(_currentEnterCoroutine);
        _currentEnterCoroutine = StartCoroutine(EnterPoseSequence(enterTrigger, enableRootMotion));
    }

    private IEnumerator EnterPoseSequence(string enterTrigger, bool enableRootMotion)
    {
        IsInPose = true;
        if (enableRootMotion)
        {
            _animator.applyRootMotion = true;
            if (_player3DMovement != null)
            {
                _player3DMovement.IsInTransitionAnimation = true;
            }
        }

        _animator.SetTrigger(enterTrigger);
        _animator.Update(0f);  // Force immediate evaluation of transitions (0 deltaTime = next "frame")
        yield return null;    // One frame for state change to propagate

        // Brief wait for transition to start
        yield return new WaitForEndOfFrame();

        // Poll for state entry (generic; customize per anim if needed)
        float maxWaitTime = 1f;
        float elapsed = 0f;
        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        bool stateEntered = false;


        while (elapsed < maxWaitTime)
        {
            if (stateInfo.IsName("Sitting Idle"))
            {
                stateEntered = true;
                break;
            }
            yield return null;
            elapsed += Time.deltaTime;
            stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        }

        if (!stateEntered)
        {
            Debug.LogWarning("EnterPoseSequence: Animator did not enter pose state within timeout!");
            if (enableRootMotion)
            {
                _animator.applyRootMotion = false;
                _player3DMovement.IsInTransitionAnimation = false;
            }
            yield break;
        }

        // Explicitly set Bool after successful state entry to sustain the loop
        _animator.SetBool("isSitting", true);
        Debug.Log("Entered Sitting Idle - Set isSitting=true");

        float animLength = stateInfo.length;
        yield return new WaitForSeconds(animLength - 0.05f);

        if (enableRootMotion)
        {
            _animator.applyRootMotion = false;
            _player3DMovement.IsInTransitionAnimation = false;
        }

        _player3DMovement?.SnapToSurface();

        Debug.Log("Pose enter complete.");
    }

    public void InstantExitPose(PoseConfig? config)
    {
        if (_currentExitCoroutine != null) StopCoroutine(_currentExitCoroutine);
        _currentExitCoroutine = StartCoroutine(InstantExitSequence(config));
    }

    private IEnumerator InstantExitSequence(PoseConfig? config)
    {
        Debug.Log($"Instant exit from pose '{CurrentPoseID}'.");

        // Black screen in (instant)
        yield return ScreenFader.Instance.FadeToBlack(duration: 0f, frameWait: 0);

        // Immediately start transition out of pose (hidden under black)
        IsInPose = false;
        _animator.SetBool("isSitting", false); // Starts blend to standing now

        float holdDuration = config?.blackHoldDuration ?? 0.5f; // Quick 0.5s
        yield return new WaitForSecondsRealtime(holdDuration);

        // Re-enable movement
        if (_playerMovement != null) _playerMovement.controlsEnabled = true;
        if (_player3DMovement != null) _player3DMovement.controlsEnabled = true;

        // Fade out
        yield return ScreenFader.Instance.FadeFromBlack(duration: 0f);

        // Reset tracking
        CurrentPoseID = null;
        _currentConfig = null;
        CurrentExitTrigger = null;

        Debug.Log("Instant exit complete.");
    }

    // Yieldable wait (generalized)
    public IEnumerator WaitForPoseComplete(bool isEnter)
    {
        yield return new WaitForSeconds(0.1f); // Buffer
        if (isEnter && _currentEnterCoroutine != null)
        {
            yield return _currentEnterCoroutine;
        }
        else if (!isEnter && _currentExitCoroutine != null)
        {
            yield return _currentExitCoroutine;
        }
        else if (!isEnter)
        {
            // For instant exit, brief wait to cover fades
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return null;
        }
    }
}

