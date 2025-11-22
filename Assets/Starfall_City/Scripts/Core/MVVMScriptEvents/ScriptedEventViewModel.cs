using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;


public class ScriptedEventViewModel : MonoBehaviour
{
    [SerializeField] private List<ScriptedEvent> eventAssets;
    [SerializeField] private EventSubscriptionConfig eventSubscriptionConfig; 
    [SerializeField] private DialogueManager_UIToolkit dialogueManager;

    private Dictionary<string, System.Action> simpleCallbacks = new Dictionary<string, System.Action>();
    private Dictionary<string, System.Action<Dictionary<string, object>>> paramCallbacks = new Dictionary<string, System.Action<Dictionary<string, object>>>();

    private bool subscriptionsSetup = false;

    private void Awake()
    {
        if (EventBus.Instance == null)
        {
            Debug.LogWarning("ScriptedEventViewModel: EventBus.Instance not ready. Retrying in Start().");
        }
        else if (!subscriptionsSetup) // Guard against dup
        {
            SetupSubscriptions();
        }
    }

    private void Start()
    {
        if (EventBus.Instance != null && !subscriptionsSetup)
        {
            SetupSubscriptions(); // safety
        }
    }

    private void SetupSubscriptions()
    {
        if (subscriptionsSetup)
        {
            Debug.LogWarning("SetupSubscriptions already called—skipping dup."); 
            return;
        }
        subscriptionsSetup = true; 

        if (eventSubscriptionConfig == null) { Debug.LogWarning("No EventSubscriptionConfig assigned!"); return; }

        foreach (var sub in eventSubscriptionConfig.subscriptions)
        {
            if (string.IsNullOrEmpty(sub.sourceEventType) || string.IsNullOrEmpty(sub.targetEventId)) continue;

            if (sub.expectsParams)
            {
                var callback = new System.Action<Dictionary<string, object>>(paramsDict =>
                    _ = ExecuteEventAsyncWithParams(sub.targetEventId, paramsDict));
                paramCallbacks[sub.sourceEventType] = callback;
                EventBus.Instance.Subscribe(sub.sourceEventType, callback);
            }
            else
            {
                var callback = new System.Action(() => _ = ExecuteEventAsync(sub.targetEventId));
                simpleCallbacks[sub.sourceEventType] = callback;
                EventBus.Instance.Subscribe(sub.sourceEventType, callback);
            }
        }
    }

    // Simple execution (no params)
    public async Task ExecuteEventAsync(string eventId)
    {
        await ExecuteEventAsyncWithParams(eventId, null);
    }

    // Parameterized execution
    public async Task ExecuteEventAsyncWithParams(string eventId, Dictionary<string, object> @params = null)
    {
        Debug.Log($"Executing event: {eventId}");
        var gameState = GameStateModel.Instance;
        if (gameState.IsEventCompleted(eventId))
        {
            Debug.Log($"Event {eventId} already completed—skipping.");
            return;
        }

        var asset = eventAssets.FirstOrDefault(e => e.eventId == eventId);
        if (asset == null) { Debug.LogWarning($"Event {eventId} not found!"); return; }

        // Runtime-safe cloning via Instantiate
        var runtimeEvent = Instantiate(asset);

        // Apply param overrides (data-driven: per-action positionParamKey)
        if (@params != null && runtimeEvent.actions != null)
        {
            foreach (var action in runtimeEvent.actions)
            {
                if (!string.IsNullOrEmpty(action.positionParamKey) &&
                    @params.TryGetValue(action.positionParamKey, out object posObj))
                {
                    action.targetPosition = (Vector3)posObj;
                }
            }
        }

        var tasks = new List<Task>();
        foreach (var action in runtimeEvent.actions)
        {
            var task = ExecuteActionAsync(action, @params);
            if (!action.parallel) await task;
            else tasks.Add(task);
        }
        await Task.WhenAll(tasks);
        gameState.SetEventCompleted(eventId);

        // Cleanup clone
        DestroyImmediate(runtimeEvent);
    }

    private async Task ExecuteActionAsync(ScriptedEvent.ActionCommand cmd, Dictionary<string, object> @params = null)
    {
        await Task.Delay((int)(cmd.delaySeconds * 1000));

        // Skip target resolution for non-object actions (e.g., StartDialogue)
        GameObject target = null;
        if (!string.IsNullOrEmpty(cmd.targetTag) && cmd.actionType != ScriptedEvent.ActionCommand.Type.StartDialogue)
        {
            target = GameObject.FindGameObjectWithTag(cmd.targetTag);
            if (target == null)
            {
                Debug.LogWarning($"Target with tag '{cmd.targetTag}' not found among active objects!");
                target = FindGameObjectWithTagIncludingInactive(cmd.targetTag);
            }
            if (target == null)
            {
                Debug.LogError($"Target with tag '{cmd.targetTag}' not found (even inactive)!");
                return;
            }
        }

        switch (cmd.actionType)
        {
            case ScriptedEvent.ActionCommand.Type.Activate:
                target.SetActive(true);
                break;
            case ScriptedEvent.ActionCommand.Type.Deactivate:
                GameObject toDeactivate = target;
                if (toDeactivate == null && @params != null && @params.TryGetValue("objectToHide", out object obj) && obj is GameObject god)
                {
                    toDeactivate = god;
                }
                if (toDeactivate != null)
                {
                    toDeactivate.SetActive(false);
                    Debug.Log($"Deactivated: {toDeactivate.name}");
                }
                break;
            case ScriptedEvent.ActionCommand.Type.Reposition:
                // Support foundPosition from params if no positionParamKey
                Vector3 pos = cmd.targetPosition;
                if (!string.IsNullOrEmpty(cmd.positionParamKey) && @params?.TryGetValue(cmd.positionParamKey, out object globalPosObj) == true)
                {
                    pos = (Vector3)globalPosObj;
                }
                else if (@params?.TryGetValue("foundPosition", out object foundPosObj) == true)
                {
                    pos = (Vector3)foundPosObj;
                }
                target.transform.position = pos;
                break;
            case ScriptedEvent.ActionCommand.Type.PlayAnimation:
                var animator = target.GetComponent<Animator>();
                if (animator) animator.Play(cmd.animationName);
                break;
            case ScriptedEvent.ActionCommand.Type.NavMeshMove:
                var agent = target.GetComponent<NavMeshAgent>();

                if (agent == null)
                {
                    Debug.LogError($"NavMeshMove: No NavMeshAgent on {target.name}!");
                    break;
                }

                // Ensure agent is enabled AND on NavMesh BEFORE setting destination
                if (!agent.isOnNavMesh)
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(target.transform.position, out hit, 5f, NavMesh.AllAreas))
                    {
                        agent.Warp(hit.position);
                        Debug.Log($"Warped {target.name} to NavMesh at {hit.position}");
                    }
                    else
                    {
                        Debug.LogError($"NavMeshMove: Cannot place {target.name} on NavMesh! Aborting move.");
                        break;
                    }
                }

                agent.enabled = true;
                agent.ResetPath();

                NavMeshHit destHit;
                Vector3 originalDest = cmd.targetPosition;
                if (NavMesh.SamplePosition(cmd.targetPosition, out destHit, 20f, NavMesh.AllAreas)) // 20m search radius
                {
                    cmd.targetPosition = destHit.position; // Snap to nearest valid
                    float snapDist = Vector3.Distance(originalDest, destHit.position);
                    Debug.Log($"NavMeshMove: Snapped dest to valid pos {destHit.position} (area ID: {destHit.mask}, snap dist: {snapDist:F2}m)");
                }
                else
                {
                    Debug.LogError($"NavMeshMove: No valid NavMesh near {cmd.targetPosition}—bake scene!");
                    break;
                }

                float initialDist = Vector3.Distance(target.transform.position, cmd.targetPosition);
                bool success = agent.SetDestination(cmd.targetPosition);

                Debug.Log($"NavMeshMove: {target.name} → {cmd.targetPosition} (dist={initialDist:F1}m, stopping={agent.stoppingDistance:F2}), SetDest success = {success}");

                if (!success)
                {
                    Debug.LogError("SetDestination failed! Path invalid or agent not ready.");
                    break;
                }

                await Task.Yield();

                var bossAnim = target.GetComponent<Animator>();
                string startParam = string.IsNullOrEmpty(cmd.animStartParam) ? cmd.animationName : cmd.animStartParam;
                if (bossAnim != null && !string.IsNullOrEmpty(startParam))
                {
                    bossAnim.SetBool(startParam, cmd.boolValue);
                    Debug.Log($"NavMeshMove: Set {startParam}={cmd.boolValue} on {target.name}");
                }

                await WaitForNavMeshReach(agent, cmd.targetPosition, cmd.tolerance + agent.stoppingDistance);

                string stopParam = string.IsNullOrEmpty(cmd.animStopParam) ? startParam : cmd.animStopParam;
                if (bossAnim != null && !string.IsNullOrEmpty(stopParam))
                {
                    bossAnim.SetBool(stopParam, !cmd.boolValue);
                    Debug.Log($"NavMeshMove: Reached—set {stopParam}=false on {target.name}, final dist={Vector3.Distance(target.transform.position, cmd.targetPosition):F1}m");
                }
                break;
            case ScriptedEvent.ActionCommand.Type.StartDialogue:
                if (dialogueManager != null)
                    dialogueManager.StartDialogue(cmd.dialogueId, cmd.speaker);
                break;
            case ScriptedEvent.ActionCommand.Type.SetPlayerControls:
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogError("SetPlayerControls: Player with tag 'Player' not found!");
                    break;
                }

                var pm = player.GetComponent<PlayerMovement>();
                var pm3d = player.GetComponent<Player3DMovement>();

                if (pm != null) pm.controlsEnabled = cmd.boolValue;
                if (pm3d != null) pm3d.controlsEnabled = cmd.boolValue;

                Debug.Log($"Player controls: {(cmd.boolValue ? "ENABLED" : "DISABLED")}");
                break;
            case ScriptedEvent.ActionCommand.Type.SetAnimationState:
                var targetAnim = target.GetComponent<Animator>();
                if (targetAnim != null) targetAnim.SetBool(cmd.paramName, cmd.boolValue);
                break;
            case ScriptedEvent.ActionCommand.Type.WaitForDialogueEnd:
                await WaitForDialogueEndAsync(cmd.dialogueId);
                break;
            case ScriptedEvent.ActionCommand.Type.WaitForReach:
                var waitAgent = target.GetComponent<NavMeshAgent>();
                if (waitAgent != null)
                {
                    Debug.Log($"WaitForReach: Starting wait for {target.name}, dest={cmd.targetPosition}, tolerance={cmd.tolerance}, initial remaining={waitAgent.remainingDistance:F1}"); // NEW: Start log
                    await WaitForNavMeshReach(waitAgent, cmd.targetPosition, cmd.tolerance);
                    Debug.Log($"WaitForReach: {target.name} reached dest! Final remaining={waitAgent.remainingDistance:F1}"); 
                }
                else
                {
                    Debug.LogError($"WaitForReach: No NavMeshAgent on {target.name}!"); 
                }
                break;
            case ScriptedEvent.ActionCommand.Type.Custom:
                switch (cmd.customSubType)
                {
                    // Find closest object handler (runs only if Custom.FindClosestObject — no effect on other CustomTypes)
                    case ScriptedEvent.ActionCommand.CustomType.FindClosestObject:
                        string hideTag = GetParamString(@params, "hideTag");
                        string refTag = GetParamString(@params, "referenceTag", "Player");

                        if (string.IsNullOrEmpty(hideTag)) break;

                        GameObject reference = GameObject.FindGameObjectWithTag(refTag) ?? GameObject.FindWithTag("Player");
                        if (reference == null) break;

                        GameObject closest = null;
                        float bestDist = float.MaxValue;

                        // Важно: ищем ВСЕ, включая неактивные!
                        var allWithTag = FindGameObjectsWithTagIncludingInactive(hideTag);
                        foreach (GameObject go in allWithTag)
                        {
                            float dist = Vector3.Distance(go.transform.position, reference.transform.position);
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                closest = go;
                            }
                        }

                        if (closest != null && @params != null)
                        {
                            @params["foundPosition"] = closest.transform.position;
                            @params["foundRotationY"] = closest.transform.eulerAngles.y;
                            @params["objectToHide"] = closest; // ← теперь можно безопасно деактивировать позже
                            Debug.Log($"[FindClosestObject] Found: {closest.name} at {closest.transform.position}");
                        }
                        break;
                    case ScriptedEvent.ActionCommand.CustomType.RotateToFace:
                        var lookAtTag = string.IsNullOrEmpty(cmd.paramName) ? "Player" : cmd.paramName;
                        var lookAtObject = GameObject.FindGameObjectWithTag(lookAtTag);
                        if (lookAtObject != null && target != null)
                        {
                            Vector3 lookDirection = lookAtObject.transform.position - target.transform.position;
                            lookDirection.y = 0;
                            if (lookDirection != Vector3.zero)
                                target.transform.rotation = Quaternion.LookRotation(lookDirection);
                        }
                        break;
                    case ScriptedEvent.ActionCommand.CustomType.ExitPose:
                        Debug.Log("ExitPose action triggered!");

                        var exitPoseTarget = string.IsNullOrEmpty(cmd.poseTargetTag)
                            ? GameObject.FindGameObjectWithTag("Player")
                            : GameObject.FindGameObjectWithTag(cmd.poseTargetTag);

                        if (exitPoseTarget == null)
                        {
                            Debug.LogError("ExitPose: Player object not found!");
                            break;
                        }

                        var playerAnim = exitPoseTarget.GetComponent<PlayerAnimation>();

                        if (PosePresenter.Instance != null)
                        {
                            // Лучший способ: передать текущий конфиг, если он есть
                            PoseConfig currentConfig = playerAnim?.currentConfig;
                            PosePresenter.Instance.ExitPose(currentConfig); // ← Совпадение сигнатуры!
                            Debug.Log($"ExitPose called with config: {currentConfig?.name ?? "null"}");
                        }
                        else
                        {
                            Debug.LogError("ExitPose: PosePresenter.Instance is null!");
                        }

                        // Ждём завершения анимации выхода
                        if (playerAnim != null)
                        {
                            await AwaitCoroutineAsync(playerAnim.WaitForPoseComplete(false));
                        }
                        else
                        {
                            await Task.Delay(2000);
                        }
                        break;
                    case ScriptedEvent.ActionCommand.CustomType.EnterPose:
                        Debug.Log("EnterPose action triggered!");
                        var enterPoseTarget = string.IsNullOrEmpty(cmd.poseTargetTag) ? GameObject.FindGameObjectWithTag("Player") : GameObject.FindGameObjectWithTag(cmd.poseTargetTag);
                        if (PosePresenter.Instance != null && enterPoseTarget != null)
                        {
                            // Resolve PoseConfig from poseID in params (if provided)
                            PoseConfig enterConfig = cmd.poseConfig;
                            string poseID = GetParamString(@params, "poseID");
                            if (!string.IsNullOrEmpty(poseID))
                            {
                                enterConfig = Resources.LoadAll<PoseConfig>("PoseConfigs").FirstOrDefault(c => c.poseID == poseID); // Adjust "PoseConfigs" folder if needed
                                if (enterConfig == null)
                                {
                                    Debug.LogWarning($"EnterPose: PoseConfig '{poseID}' not found — using default");
                                    enterConfig = ScriptableObject.CreateInstance<PoseConfig>();
                                    enterConfig.poseID = "DefaultPose";
                                }
                            }
                            else if (enterConfig == null && @params != null && @params.TryGetValue("poseConfig", out object configObj))
                            {
                                enterConfig = configObj as PoseConfig;
                            }
                            if (enterConfig == null)
                            {
                                enterConfig = ScriptableObject.CreateInstance<PoseConfig>(); // Fallback
                                enterConfig.poseID = "DefaultEnter";
                                enterConfig.enterTrigger = "Sit"; // Basic defaults
                                enterConfig.blackHoldDuration = 2f;
                            }

                            // Resolve position: From cmd or params
                            Vector3 enterPos = cmd.targetPosition;
                            if (@params != null && @params.TryGetValue(cmd.positionParamKey, out object posObj))
                            {
                                enterPos = (Vector3)posObj;
                            }
                            // Support foundRotationY from params
                            float enterYRot = cmd.targetPosition.y; // Fallback to pos Y
                            if (!string.IsNullOrEmpty(cmd.rotationParamKey) && @params != null && @params.TryGetValue(cmd.rotationParamKey, out object rotObj))
                            {
                                enterYRot = (float)rotObj;
                            }
                            else if (@params != null && @params.TryGetValue("foundRotationY", out object foundRotObj))
                            {
                                enterYRot = (float)foundRotObj;
                            }

                            // Use reflection for methodName if dynamic; fallback to direct
                            MethodInfo enterMethod = typeof(PosePresenter).GetMethod(cmd.enterMethodName, BindingFlags.Public | BindingFlags.Instance);
                            if (enterMethod != null)
                            {
                                enterMethod.Invoke(PosePresenter.Instance, new object[] { enterPos, enterYRot, enterConfig });
                            }
                            else
                            {
                                PosePresenter.Instance.EnterPose(enterPos, enterYRot, enterConfig);  // Fallback
                            }

                            // Just wait for the transition to complete (black screen duration)
                            if (enterConfig != null)
                            {
                                await Task.Delay((int)(enterConfig.blackHoldDuration * 1000) + 500); // Add small buffer
                            }
                            else
                            {
                                await Task.Delay(2000); // Fallback wait
                            }

                            Debug.Log("EnterPose completed - ready for ExitPose");
                        }
                        else
                        {
                            Debug.LogWarning("EnterPose: PosePresenter or Player target missing!");
                        }
                        break;
                    case ScriptedEvent.ActionCommand.CustomType.FadeBlackFlash:
                        if (ScreenFader.Instance != null)
                        {
                            await ScreenFader.Instance.FadeToBlackAsync(duration: 0f); // Add this async method to ScreenFader if missing
                            await Task.Delay(1000, default); // 1s hold (ms); for realtime, wrap WaitForSecondsRealtime via coroutine if needed
                            await ScreenFader.Instance.FadeFromBlackAsync(duration: 0f);
                        }
                        else
                        {
                            Debug.LogWarning("FadeBlackFlash: ScreenFader.Instance missing!");
                        }
                        break;
                    // Extensible: Add cases for new CustomType enum values
                    default:
                        Debug.LogWarning($"Unknown CustomType: {cmd.customSubType}");
                        break;
                }
                break;
        }
    }

    private string GetParamString(Dictionary<string, object> paramsDict, string key, string fallback = "")
    {
        if (paramsDict != null && paramsDict.TryGetValue(key, out object val) && val is string str)
            return str;
        return fallback;
    }

    private GameObject FindGameObjectWithTagIncludingInactive(string tag)
    {
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();

        foreach (var root in rootObjects)
        {
            // Search this root and all children, including inactive
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in transforms)
            {
                if (t.CompareTag(tag))
                    return t.gameObject;
            }
        }
        return null;
    }

    private GameObject[] FindGameObjectsWithTagIncludingInactive(string tag)
    {
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();
        var results = new List<GameObject>();

        foreach (var root in rootObjects)
        {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in transforms)
            {
                if (t.CompareTag(tag))
                    results.Add(t.gameObject);
            }
        }
        return results.ToArray();
    }

    private async Task AwaitCoroutineAsync(IEnumerator routine)
    {
        if (routine == null)
        {
            Debug.LogWarning("AwaitCoroutineAsync: Routine null—skipping.");
            return;
        }
        Debug.Log($"Awaiting coroutine: {routine.GetType().Name}"); // NEW: Track calls

        var tcs = new TaskCompletionSource<object>();
        StartCoroutine(new AwaitWrapper(routine, tcs));
        await tcs.Task;
    }

    private class AwaitWrapper : IEnumerator
    {
        private readonly IEnumerator _innerRoutine;
        private readonly TaskCompletionSource<object> _tcs;

        public AwaitWrapper(IEnumerator innerRoutine, TaskCompletionSource<object> tcs)
        {
            _innerRoutine = innerRoutine;
            _tcs = tcs;
        }

        public object Current => _innerRoutine.Current;

        public bool MoveNext() => _innerRoutine.MoveNext();

        public void Reset() => _innerRoutine.Reset();

        public void Dispose()
        {
            _innerRoutine.MoveNext();
            if (!_tcs.Task.IsCompleted) _tcs.SetResult(null);
        }
    }

    private Task WaitForDialogueEndAsync(string expectedDialogueId)
    {
        var tcs = new TaskCompletionSource<bool>();
        Action handler = null;

        // Local handler to avoid capturing in lambda
        handler = () =>
        {
            bool shouldComplete = string.IsNullOrEmpty(expectedDialogueId) ||
                              (dialogueManager != null && dialogueManager.currentStartID == expectedDialogueId);

            if (shouldComplete)
            {
                DialogueManager_UIToolkit.OnDialogueEnded -= handler;
                tcs.TrySetResult(true);
                Debug.Log($"[ScriptedEvent] WaitForDialogueEnd completed for: '{expectedDialogueId}'");
            }
        };

        DialogueManager_UIToolkit.OnDialogueEnded += handler;

        // Immediate check in case dialogue already ended
        if (dialogueManager == null || string.IsNullOrEmpty(dialogueManager.currentStartID))
        {
            handler();
        }

        // Cancelable timeout
        var cts = new System.Threading.CancellationTokenSource();
        var timeoutTask = Task.Delay(30000, cts.Token);

        // When timeout fires
        timeoutTask.ContinueWith(t =>
        {
            if (!t.IsCanceled && !tcs.Task.IsCompleted)
            {
                DialogueManager_UIToolkit.OnDialogueEnded -= handler;
                Debug.LogWarning($"[ScriptedEvent] WaitForDialogueEnd TIMED OUT after 30s for dialogue ID: '{expectedDialogueId}'");
                tcs.TrySetResult(false); // Unblock sequence
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());

        // When real event fires → cancel timeout
        tcs.Task.ContinueWith(t =>
        {
            cts.Cancel(); // This prevents the timeout warning from appearing
        }, TaskContinuationOptions.OnlyOnRanToCompletion);

        return tcs.Task;
    }

    private async Task WaitForNavMeshReach(NavMeshAgent agent, Vector3 destination, float tolerance)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(WaitCoroutine(agent, destination, tolerance, tcs));
        await tcs.Task;
    }

    private IEnumerator WaitCoroutine(NavMeshAgent agent, Vector3 destination, float extraTolerance, TaskCompletionSource<bool> tcs)
    {
        float threshold = agent.stoppingDistance + extraTolerance; // e.g., 0.1 + 0.0 = 0.1
        int frames = 0;
        const int maxFrames = 600;

        Debug.Log($"Wait start: Threshold={threshold:F2}m, pending={agent.pathPending}, status={agent.pathStatus}, initial dist={Vector3.Distance(agent.transform.position, destination):F2}m");

        // Phase 1: Wait for path to be ready
        while (agent.pathPending && frames < maxFrames)
        {
            frames++;
            if (frames % 30 == 0) Debug.Log($"Path calc [{frames}]: Still pending...");
            yield return null;
        }

        if (agent.pathPending)
        {
            Debug.LogWarning($"Wait: Path timeout—aborting.");
            tcs.SetResult(false);
            yield break;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogError($"Wait: Path invalid—aborting move for {agent.gameObject.name}");
            tcs.SetResult(false);
            yield break;
        }

        // Sometimes agent reports path complete but hasn't begun movement yet
        Vector3 startPos = agent.transform.position;
        int startupFrames = 0;
        const int maxStartupFrames = 30;

        while (startupFrames < maxStartupFrames &&
               Vector3.Distance(agent.transform.position, startPos) < 0.01f)
        {
            startupFrames++;
            if (startupFrames % 10 == 0)
            {
                Debug.Log($"Startup wait [{startupFrames}]: Agent not moving yet, pos delta={Vector3.Distance(agent.transform.position, startPos):F3}m");
            }
            yield return null;
        }

        // Phase 2: Wait for actual movement (position-based)
        frames = 0;
        float currentDist = Vector3.Distance(agent.transform.position, destination);

        while (agent.enabled && agent.isOnNavMesh && currentDist > threshold && frames < maxFrames)
        {
            frames++;
            currentDist = Vector3.Distance(agent.transform.position, destination);

            if (frames % 30 == 0)
            {
                Debug.Log($"Move [{frames}]: Current dist={currentDist:F2}m, velocity={agent.velocity.magnitude:F1}m/s, reported remaining={agent.remainingDistance:F2}m");

                // Force path recalculation if stuck
                if (agent.velocity.magnitude < 0.1f && frames > 60)
                {
                    Debug.Log($"Agent appears stuck - resetting destination");
                    agent.ResetPath();
                    agent.SetDestination(destination);
                }
            }
            yield return null;
        }

        bool reached = currentDist <= threshold;
        if (!reached)
        {
            Debug.LogWarning($"Wait: Timeout after {frames} frames. Final dist={currentDist:F2}m, velocity={agent.velocity.magnitude:F1}. Agent enabled={agent.enabled}, onNavMesh={agent.isOnNavMesh}");
        }
        else
        {
            Debug.Log($"Wait: Reached destination! Final dist={currentDist:F2}m");
        }
        tcs.SetResult(reached);
    }

    private void OnDestroy()
    {
        // Data-driven unsubscribe
        foreach (var kvp in simpleCallbacks)
        {
            EventBus.Instance.Unsubscribe(kvp.Key, kvp.Value);
        }
        foreach (var kvp in paramCallbacks)
        {
            EventBus.Instance.Unsubscribe(kvp.Key, kvp.Value);
        }
    }
}