using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;

public class ScriptedEventViewModel : MonoBehaviour
{
    [SerializeField] private List<ScriptedEvent> eventAssets;
    [SerializeField] private DialogueManager_UIToolkit dialogueManager;

    private void Start()
    {
        // Subscribe to simple triggers
        EventBus.Instance.Subscribe("SceneLoaded:Office", () => ExecuteEventAsync("PreIntro"));
        EventBus.Instance.Subscribe("DialogueEnded:Intro", () => ExecuteEventAsync("PostDialogueIntro"));

        // Parameterized for IntroStart
        EventBus.Instance.Subscribe("IntroStart", (Dictionary<string, object> @params) =>
            ExecuteEventAsyncWithParams("IntroStart", @params));
    }

    // Simple execution (no params)
    public async void ExecuteEventAsync(string eventId)
    {
        await ExecuteEventAsyncWithParams(eventId, null);
    }

    // Parameterized execution
    public async void ExecuteEventAsyncWithParams(string eventId, Dictionary<string, object> @params = null)
    {
        var gameState = GameStateModel.Instance;
        if (gameState.IsEventCompleted(eventId)) return;

        var asset = eventAssets.FirstOrDefault(e => e.eventId == eventId);
        if (asset == null) { Debug.LogWarning($"Event {eventId} not found!"); return; }

        // Clone asset to runtime instance (preserves original)
        var runtimeEvent = ScriptableObject.CreateInstance<ScriptedEvent>();
        UnityEngine.Object[] assetProps = new UnityEngine.Object[1] { asset };
        UnityEditor.EditorUtility.CopySerializedValuesFromPrefab(assetProps, new UnityEngine.Object[1] { runtimeEvent });
        // Note: CopySerializedValuesFromPrefab works for ScriptableObjects too; if not, manual copy via JsonUtility or reflection

        // Apply param overrides (e.g., positions)
        if (@params != null)
        {
            foreach (var action in runtimeEvent.actions)
            {
                if (@params.TryGetValue("deskPos", out object deskPosObj) && action.actionType == ScriptedEvent.ActionCommand.Type.NavMeshMove)
                    action.targetPosition = (Vector3)deskPosObj;
                if (@params.TryGetValue("bossInitialPos", out object initialPosObj) && action.actionType == ScriptedEvent.ActionCommand.Type.NavMeshMove)
                    action.targetPosition = (Vector3)initialPosObj;
                // Extend for more keys as needed
            }
        }

        var tasks = new List<Task>();
        foreach (var action in runtimeEvent.actions)
        {
            var task = ExecuteActionAsync(action);
            if (!action.parallel) await task;
            else tasks.Add(task);
        }
        await Task.WhenAll(tasks);
        gameState.SetEventCompleted(eventId);

        // Cleanup clone
        DestroyImmediate(runtimeEvent);
    }

    private async Task ExecuteActionAsync(ScriptedEvent.ActionCommand cmd)
    {
        await Task.Delay((int)(cmd.delaySeconds * 1000));

        // Resolve target at runtime
        if (string.IsNullOrEmpty(cmd.targetTag))
        {
            Debug.LogWarning($"Action {cmd.actionType} missing targetTag!");
            return;
        }
        GameObject target = GameObject.FindGameObjectWithTag(cmd.targetTag);
        if (target == null)
        {
            Debug.LogError($"Target with tag '{cmd.targetTag}' not found!");
            return;
        }

        switch (cmd.actionType)
        {
            case ScriptedEvent.ActionCommand.Type.Activate:
                target.SetActive(true);
                break;
            case ScriptedEvent.ActionCommand.Type.Deactivate:
                target.SetActive(false);
                break;
            case ScriptedEvent.ActionCommand.Type.Reposition:
                target.transform.position = cmd.targetPosition;
                break;
            case ScriptedEvent.ActionCommand.Type.PlayAnimation:
                var animator = target.GetComponent<Animator>();
                if (animator) animator.Play(cmd.animationName);
                break;
            case ScriptedEvent.ActionCommand.Type.NavMeshMove:
                var agent = target.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = true;
                    agent.ResetPath();
                    agent.stoppingDistance = cmd.tolerance;
                    agent.SetDestination(cmd.targetPosition);
                    var bossAnim = target.GetComponent<Animator>();
                    if (bossAnim != null) bossAnim.SetBool("isWalking", true);
                    await WaitForNavMeshReach(agent, cmd.targetPosition, cmd.tolerance);
                    if (bossAnim != null) bossAnim.SetBool("isWalking", false);
                }
                break;
            case ScriptedEvent.ActionCommand.Type.StartDialogue:
                if (dialogueManager != null)
                    dialogueManager.StartDialogue(cmd.dialogueId, cmd.speaker);
                break;
            case ScriptedEvent.ActionCommand.Type.SetPlayerControls:
                var playerMovement = target.GetComponent<PlayerMovement>();
                var player3DMovement = target.GetComponent<Player3DMovement>();
                if (playerMovement != null) playerMovement.controlsEnabled = cmd.boolValue;
                if (player3DMovement != null) player3DMovement.controlsEnabled = cmd.boolValue;
                break;
            case ScriptedEvent.ActionCommand.Type.SetAnimationState:
                var targetAnim = target.GetComponent<Animator>();
                if (targetAnim != null) targetAnim.SetBool(cmd.paramName, cmd.boolValue);
                break;
            case ScriptedEvent.ActionCommand.Type.WaitForReach:
                var waitAgent = target.GetComponent<NavMeshAgent>();
                if (waitAgent != null)
                    await WaitForNavMeshReach(waitAgent, cmd.targetPosition, cmd.tolerance);
                break;
            case ScriptedEvent.ActionCommand.Type.Custom:
                // e.g., For boss rotation: if (cmd.targetTag == "Boss") { /* logic */ }
                // Or for PosePresenter: if (cmd.animationName == "ExitPose") PosePresenter.Instance.ExitPose();
                Debug.Log($"Custom action for tag {cmd.targetTag}");
                break;
        }
    }

    // WaitForNavMeshReach and WaitCoroutine unchanged from previous
    private async Task WaitForNavMeshReach(NavMeshAgent agent, Vector3 destination, float tolerance)
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
        StartCoroutine(WaitCoroutine(agent, destination, tolerance, tcs));
        await tcs.Task;
    }

    private IEnumerator WaitCoroutine(NavMeshAgent agent, Vector3 destination, float tolerance, System.Threading.Tasks.TaskCompletionSource<bool> tcs)
    {
        while (agent.enabled && (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + tolerance))
        {
            if (Time.frameCount % 120 == 0)
                Debug.Log($"ScriptedEvent: Agent remaining distance: {agent.remainingDistance}");
            yield return null;
        }
        tcs.SetResult(true);
    }
}
