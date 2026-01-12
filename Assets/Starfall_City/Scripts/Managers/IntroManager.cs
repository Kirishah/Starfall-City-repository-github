using DialogueSystem;
using core;
using Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float _bossApproachDelay = 2f;

    [Header("Positions")]
    [SerializeField] private float _bossStoppingDistance = 0.2f;
    [SerializeField] private float _minStartDistance = 3f;

    private GameObject _player;
    private GameObject _boss;
    private Vector3 _bossInitialPos;
    private Vector3 _deskPosition;
    private bool _isInitialized = false;

    void Start() => Initialize();

    private void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        if (!ResolveDynamicReferences())
        {
            StartCoroutine(RetryInitialization());
            return;
        }

        // Lightweight validation
        var playerMovement = _player.GetComponent<PlayerMovement>();
        var player3DMovement = _player.GetComponent<Player3DMovement>();
        if (playerMovement == null && player3DMovement == null)
        {
            Debug.LogError("IntroManager: No valid PlayerMovement or Player3DMovement on Player!");
            enabled = false;
            return;
        }

        if (!_boss.TryGetComponent<NavMeshAgent>(out var bossAgent))
        {
            Debug.LogError("IntroManager: NavMeshAgent missing on Boss NPC!");
            enabled = false;
            return;
        }

        bossAgent.enabled = true;
        bossAgent.stoppingDistance = _bossStoppingDistance;

        // Subscribe
        DialogueManager_UIToolkit.OnDialogueEnded += HandleDialogueEnd;

        // Trigger pre-intro (no params needed)
        EventBus.Instance.Publish("SceneLoaded:Mansion");

        // Delayed intro with params
        StartCoroutine(DelayedIntroStart());

        Debug.Log("IntroManager: Initialized and events queued.");
    }

    private bool ResolveDynamicReferences()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        if (_player == null) return false;

        _boss = GameObject.FindGameObjectWithTag("Boss");
        if (_boss == null) return false;

        _bossInitialPos = _boss.transform.position;

        var potentialDesks = GameObject.FindGameObjectsWithTag("Desk");
        if (potentialDesks.Length == 0)
        {
            _deskPosition = _player.transform.position + (_player.transform.forward * 2f); // Fallback position
            Debug.LogWarning("IntroManager: No Desk found; using fallback position.");
        }
        else
        {
            Transform closest = null;
            float minDist = float.MaxValue;
            foreach (var deskObj in potentialDesks)
            {
                float dist = Vector3.Distance(_player.transform.position, deskObj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = deskObj.transform;
                }
            }
            _deskPosition = closest != null ? closest.position : _player.transform.position + (_player.transform.forward * 2f);
            Debug.Log($"IntroManager: Selected desk at distance {minDist}.");
        }

        // Validate distance (unchanged)
        float distBetweenPositions = Vector3.Distance(_bossInitialPos, _deskPosition);
        if (distBetweenPositions < _minStartDistance)
        {
            _bossInitialPos = _player.transform.position - (_player.transform.forward * 8f) + (_player.transform.right * 2f);
            distBetweenPositions = Vector3.Distance(_bossInitialPos, _deskPosition);
            Debug.Log($"IntroManager: Adjusted initial pos. New distance: {distBetweenPositions:F2}m.");
        }

        return true;
    }

    private IEnumerator RetryInitialization()
    {
        yield return null; // One frame
        Initialize();
    }

    private IEnumerator DelayedIntroStart()
    {
        yield return new WaitForSeconds(_bossApproachDelay);
        var paramsDict = new Dictionary<string, object>
        {
            { "deskPos", _deskPosition }
            // bossInitialPos not needed here
        };
        EventBus.Instance.Publish("IntroStart", paramsDict);
    }

    public void HandleDialogueEnd()
    {
        var returnParams = new Dictionary<string, object>
        {
            { "bossInitialPos", _bossInitialPos }
        };
        EventBus.Instance.Publish("DialogueEnded:Intro", returnParams);
        DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;
    }

    void OnDestroy() => DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;

    // Scene handling (unchanged)
    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => _isInitialized = false;
}
