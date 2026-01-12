using DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace core
{
    public class ScriptedEventExecutor : MonoBehaviour
    {
        [SerializeField] private List<ScriptedEvent> eventAssets;
        [SerializeField] private DialogueManager_UIToolkit dialogueManager;

        private readonly Dictionary<ScriptedEvent.ActionCommand.Type, IScriptedActionHandler> _handlers = new();
        private readonly List<(GameObject obj, string spawnId, float yRotation)> _pendingTransfers = new();

        private void Awake()
        {
            // Register all handlers here (you'll add more over time)
            RegisterHandler(new ActivateActionHandler());
            RegisterHandler(new DeactivateActionHandler());
            RegisterHandler(new LoadSceneActionHandler(this));
            RegisterHandler(new TransferObjActionHandler(this));
            RegisterHandler(new NavMeshMoveActionHandler());
            RegisterHandler(new StartDialogueActionHandler(dialogueManager));
            RegisterHandler(new WaitForDialogueEndActionHandler());
            RegisterHandler(new WaitForReachActionHandler());
            RegisterHandler(new SetPlayerControlsActionHandler());
            RegisterHandler(new RepositionActionHandler());
            RegisterHandler(new AnimationActionHandler());
            RegisterHandler(new SetAnimActionHandler());
            RegisterHandler(new StartQTEActionHandler());
            RegisterHandler(new CustomActionHandler(this, dialogueManager)); // handles all Custom subtypes
        }

        private void RegisterHandler(IScriptedActionHandler handler) => _handlers[handler.SupportedType] = handler;

        public async UniTask ExecuteEventAsync(string eventId, Dictionary<string, object> @params = null)
        {
            Debug.Log($"[Executor] Executing event: {eventId}");

            var gameState = GameStateModel.Instance;
            if (gameState.IsEventCompleted(eventId))
            {
                Debug.Log($"[Executor] Event {eventId} already completed—skipping.");
                return;
            }

            var asset = eventAssets.FirstOrDefault(e => e.eventId == eventId);
            if (asset == null)
            {
                Debug.LogWarning($"[Executor] Event asset '{eventId}' not found!");
                return;
            }

            var runtimeEvent = Instantiate(asset);

            // Apply parameter overrides for positions
            if (@params != null && runtimeEvent.actions != null)
            {
                foreach (var action in runtimeEvent.actions)
                {
                    if (!string.IsNullOrEmpty(action.positionParamKey) &&
                        @params.TryGetValue(action.positionParamKey, out var posObj) &&
                        posObj is Vector3 pos)
                    {
                        action.targetPosition = pos;
                    }
                }
            }

            var parallelTasks = new List<UniTask>();
            foreach (var action in runtimeEvent.actions)
            {
                var task = ExecuteSingleActionAsync(action, @params);
                if (action.parallel)
                    parallelTasks.Add(task);
                else
                    await task;
            }

            if (parallelTasks.Count > 0)
                await UniTask.WhenAll(parallelTasks);

            gameState.SetEventCompleted(eventId);
            DestroyImmediate(runtimeEvent);
        }

        private async UniTask ExecuteSingleActionAsync(ScriptedEvent.ActionCommand cmd, Dictionary<string, object> @params)
        {
            if (cmd.delaySeconds > 0)
                await UniTask.Delay((int)(cmd.delaySeconds * 1000));

            GameObject target = ResolveTarget(cmd);

            if (_handlers.TryGetValue(cmd.actionType, out var handler))
            {
                await handler.ExecuteAsync(cmd, target, @params, this);
            }
            else
            {
                Debug.LogWarning($"[Executor] No handler registered for action type: {cmd.actionType}");
            }
        }

        private GameObject ResolveTarget(ScriptedEvent.ActionCommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.targetTag) || cmd.actionType == ScriptedEvent.ActionCommand.Type.StartDialogue)
                return null;

            var target = GameObject.FindGameObjectWithTag(cmd.targetTag);
            if (target == null) target = this.FindWithTagIncludingInactive(cmd.targetTag);
            if (target == null) target = this.FindWithTagInAllScenes(cmd.targetTag);

            if (target == null)
                Debug.LogError($"[Executor] Target with tag '{cmd.targetTag}' not found anywhere!");

            return target;
        }

        // === Shared state methods for handlers ===
        public void QueueTransfer(GameObject obj, string spawnId, float yRotation) => _pendingTransfers.Add((obj, spawnId, yRotation));

        public async UniTask ResolvePendingTransfersAsync()
        {
            // Copy your existing ResolvePendingTransfers logic here (almost identical)
            if (_pendingTransfers.Count == 0) return;

            await UniTask.NextFrame();

            var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            var spawnDict = spawnPoints.ToDictionary(sp => sp.spawnId, sp => sp);

            foreach (var (obj, spawnId, yRot) in _pendingTransfers)
            {
                if (obj == null)
                {
                    Debug.LogWarning("[EVENT EXECUTOR]: Object is missing.");
                    continue;
                }
                if (!spawnDict.TryGetValue(spawnId, out var spawn))
                {
                    Debug.LogError($"SpawnPoint '{spawnId}' not found!");
                    continue;
                }

                obj.transform.position = spawn.transform.position;
                if (!Mathf.Approximately(yRot, 0f))
                {
                    var rot = obj.transform.eulerAngles;
                    rot.y = yRot;
                    obj.transform.eulerAngles = rot;
                }
                else
                {
                    obj.transform.rotation = spawn.transform.rotation;
                }

                if (!obj.activeInHierarchy) obj.SetActive(true);

                obj.GetComponent<NavMeshAgent>().EnterCinematicMode();

                if (obj.TryGetComponent<DontDestroyOnLoadTag>(out var ddolTag))
                    StartCoroutine(RemoveDDOLNextFrame(ddolTag));
            }

            _pendingTransfers.Clear();
        }

        private static IEnumerator RemoveDDOLNextFrame(DontDestroyOnLoadTag tag)
        {
            yield return null;
            tag.MakeNormalAgain();
        }

        // Keep AwaitCoroutineAsync here if still needed (for ExitPose)
        public async UniTask AwaitCoroutineAsync(IEnumerator routine)
        {
            if (routine == null) return;

            // Critical: Check if this MonoBehaviour is still alive
            if (this == null || gameObject == null || !gameObject.activeInHierarchy)
            {
                Debug.LogWarning("AwaitCoroutineAsync: ScriptedEventExecutor was destroyed before awaiting coroutine.");
                return;
            }

            var tcs = new UniTaskCompletionSource<object>();

            // Start the coroutine only if we're still valid
            if (this != null)
            {
                StartCoroutine(AwaitWrapper(routine, tcs));
            }
            else
            {
                tcs.TrySetCanceled();
                return;
            }

            await tcs.Task;
        }

        private IEnumerator AwaitWrapper(IEnumerator inner, UniTaskCompletionSource<object> tcs)
        {
            while (inner.MoveNext())
                yield return inner.Current;

            tcs.TrySetResult(null);
        }
    }
}
