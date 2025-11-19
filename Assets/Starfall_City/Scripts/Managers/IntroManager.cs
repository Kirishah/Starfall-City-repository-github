using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueManager_UIToolkit dialogueManager;

    [Header("Timing")]
    [SerializeField] private float bossApproachDelay = 6f;

    [Header("Positions")]
    [SerializeField] private float bossStoppingDistance = 1.5f;
    [SerializeField] private float minStartDistance = 3f;

    [Header("Events")]
    [SerializeField] private ScriptedEvent preIntroEvent; // Tags: "Player"
    [SerializeField] private ScriptedEvent introStartEvent; // Tags: "Boss" for move/wait; no tag for dialogue
    [SerializeField] private ScriptedEvent postDialogueEvent; // Tags: "Player", "Boss"

    private GameObject player;
    private GameObject boss;
    private Vector3 bossInitialPos;
    private Vector3 deskPosition;
    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (!ResolveDynamicReferences())
        {
            StartCoroutine(RetryInitialization());
            return;
        }

        // Lightweight validation
        var playerMovement = player.GetComponent<PlayerMovement>();
        var player3DMovement = player.GetComponent<Player3DMovement>();
        if (playerMovement == null && player3DMovement == null)
        {
            Debug.LogError("IntroManager: No valid PlayerMovement or Player3DMovement on Player!");
            enabled = false;
            return;
        }

        var bossAgent = boss.GetComponent<NavMeshAgent>();
        if (bossAgent == null)
        {
            Debug.LogError("IntroManager: NavMeshAgent missing on Boss NPC!");
            enabled = false;
            return;
        }

        bossAgent.enabled = true;
        bossAgent.stoppingDistance = bossStoppingDistance;

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
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss == null) return false;

        bossInitialPos = boss.transform.position;

        GameObject[] potentialDesks = GameObject.FindGameObjectsWithTag("Desk");
        if (potentialDesks.Length == 0)
        {
            deskPosition = player.transform.position + player.transform.forward * 2f; // Fallback position
            Debug.LogWarning("IntroManager: No Desk found; using fallback position.");
        }
        else
        {
            Transform closest = null;
            float minDist = float.MaxValue;
            foreach (var deskObj in potentialDesks)
            {
                float dist = Vector3.Distance(player.transform.position, deskObj.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = deskObj.transform;
                }
            }
            deskPosition = closest != null ? closest.position : player.transform.position + player.transform.forward * 2f;
            Debug.Log($"IntroManager: Selected desk at distance {minDist}.");
        }

        // Validate distance (unchanged)
        float distBetweenPositions = Vector3.Distance(bossInitialPos, deskPosition);
        if (distBetweenPositions < minStartDistance)
        {
            bossInitialPos = player.transform.position - (player.transform.forward * 8f) + (player.transform.right * 2f);
            distBetweenPositions = Vector3.Distance(bossInitialPos, deskPosition);
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
        yield return new WaitForSeconds(bossApproachDelay);
        var paramsDict = new Dictionary<string, object>
        {
            { "deskPos", deskPosition }
            // bossInitialPos not needed here
        };
        EventBus.Instance.Publish("IntroStart", paramsDict);
    }

    public void HandleDialogueEnd()
    {
        var returnParams = new Dictionary<string, object>
        {
            { "bossInitialPos", bossInitialPos }
        };
        EventBus.Instance.Publish("DialogueEnded:Intro", returnParams);
        DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;
    }

    void OnDestroy()
    {
        DialogueManager_UIToolkit.OnDialogueEnded -= HandleDialogueEnd;
    }

    // Scene handling (unchanged)
    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => isInitialized = false;
}