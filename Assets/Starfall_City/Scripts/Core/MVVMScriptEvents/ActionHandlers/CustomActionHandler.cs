using DialogueSystem;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Interaction;

namespace core
{
    public class CustomActionHandler : MonoBehaviour, IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.Custom;

        private readonly ScriptedEventExecutor _executor;
        private readonly DialogueManager_UIToolkit _dialogueManager;

        public CustomActionHandler(ScriptedEventExecutor executor, DialogueManager_UIToolkit dialogueManager)
        {
            _executor = executor;
            _dialogueManager = dialogueManager;
        }

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> @params, ScriptedEventExecutor __)
        {
            switch (cmd.customSubType)
            {
                case ScriptedEvent.ActionCommand.CustomType.FindClosestObject:
                    HandleFindClosestObject(cmd, @params);
                    break;

                case ScriptedEvent.ActionCommand.CustomType.RotateToFace:
                    HandleRotateToFace(cmd, target);
                    break;

                case ScriptedEvent.ActionCommand.CustomType.ExitPose:
                    await HandleExitPoseAsync(cmd, _executor);
                    break;

                case ScriptedEvent.ActionCommand.CustomType.EnterPose:
                    await HandleEnterPoseAsync(cmd, @params, _executor);
                    break;

                case ScriptedEvent.ActionCommand.CustomType.FadeBlackFlash:
                    await HandleFadeBlackFlashAsync();
                    break;

                default:
                    Debug.LogWarning($"Unknown CustomType: {cmd.customSubType}");
                    break;
            }
        }

        private void HandleFindClosestObject(ScriptedEvent.ActionCommand cmd, Dictionary<string, object> @params)
        {
            var hideTag = @params.GetParamString("hideTag");
            var refTag = @params.GetParamString("referenceTag", "Player");

            if (string.IsNullOrEmpty(hideTag)) return;

            var reference = GameObject.FindGameObjectWithTag(refTag);

            if (reference == null)
            {
                reference = GameObject.FindWithTag("Player");
            }

            if (reference == null) return;

            GameObject closest = null;
            var bestDist = float.MaxValue;

            var allWithTag = this.FindAllWithTagIncludingInactive(hideTag); // using your extension
            foreach (var go in allWithTag)
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
                @params["objectToHide"] = closest;
                Debug.Log($"[FindClosestObject] Found: {closest.name}");
            }
        }

        private void HandleRotateToFace(ScriptedEvent.ActionCommand cmd, GameObject target)
        {
            var lookAtTag = string.IsNullOrEmpty(cmd.paramName) ? "Player" : cmd.paramName;
            var lookAt = GameObject.FindGameObjectWithTag(lookAtTag);
            if (lookAt != null && target != null)
            {
                Vector3 dir = lookAt.transform.position - target.transform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                    target.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        private async UniTask HandleExitPoseAsync(ScriptedEvent.ActionCommand cmd, ScriptedEventExecutor executor)
        {
            var player = string.IsNullOrEmpty(cmd.poseTargetTag)
                ? GameObject.FindGameObjectWithTag("Player")
                : GameObject.FindGameObjectWithTag(cmd.poseTargetTag);

            if (player == null)
            {
                Debug.LogError("ExitPose: Player not found!");
                return;
            }

            var playerAnim = player.GetComponent<PlayerAnimation>();

            if (PosePresenter.Instance != null)
            {
                var config = playerAnim.CurrentConfig;
                PosePresenter.Instance.ExitPose(config);
            }

            if (playerAnim != null)
                await executor.AwaitCoroutineAsync(playerAnim.WaitForPoseComplete(false));
            else
                await UniTask.Delay(2000);
        }

        private async UniTask HandleEnterPoseAsync(
            ScriptedEvent.ActionCommand cmd,
            Dictionary<string, object> @params,
            ScriptedEventExecutor executor)
        {
            var player = string.IsNullOrEmpty(cmd.poseTargetTag)
                ? GameObject.FindGameObjectWithTag("Player")
                : GameObject.FindGameObjectWithTag(cmd.poseTargetTag);

            if (PosePresenter.Instance == null || player == null)
            {
                Debug.LogWarning("EnterPose: Missing presenter or player!");
                return;
            }

            var enterConfig = cmd.poseConfig;

            var poseID = @params.GetParamString("poseID");
            if (!string.IsNullOrEmpty(poseID))
            {
                var configs = Resources.LoadAll<PoseConfig>("PoseConfigs");
                enterConfig = configs.FirstOrDefault(c => c.poseID == poseID);

                if (enterConfig == null)
                {
                    Debug.LogWarning($"EnterPose: PoseConfig '{poseID}' not found — using default");
                    enterConfig = ScriptableObject.CreateInstance<PoseConfig>();
                    enterConfig.poseID = "DefaultPose";
                }
            }
            else if (enterConfig == null && @params != null && @params.TryGetValue("poseConfig", out var configObj) && configObj is PoseConfig pc)
            {
                enterConfig = pc;
            }

            if (enterConfig == null)
            {
                enterConfig = ScriptableObject.CreateInstance<PoseConfig>();
                enterConfig.poseID = "DefaultEnter";
                enterConfig.enterTrigger = "Sit";
                enterConfig.blackHoldDuration = 2f;
            }

            // Resolve position
            Vector3 enterPos = cmd.targetPosition;
            if (!string.IsNullOrEmpty(cmd.positionParamKey) && @params != null && @params.TryGetValue(cmd.positionParamKey, out var posObj) && posObj is Vector3 vPos)
            {
                enterPos = vPos;
            }
            else if (@params != null && @params.TryGetValue("foundPosition", out var foundPos) && foundPos is Vector3 fPos)
            {
                enterPos = fPos;
            }

            // Resolve rotation Y
            float enterYRot = 0f;
            if (@params != null && @params.TryGetValue("foundRotationY", out var rotObj) && rotObj is float fRot)
            {
                enterYRot = fRot;
            }
            else if (!string.IsNullOrEmpty(cmd.rotationParamKey) && @params != null && @params.TryGetValue(cmd.rotationParamKey, out var rObj) && rObj is float rotVal)
            {
                enterYRot = rotVal;
            }

            // Call EnterPose (direct or via reflection)
            MethodInfo enterMethod = typeof(PosePresenter).GetMethod(cmd.enterMethodName, BindingFlags.Public | BindingFlags.Instance);
            if (enterMethod != null)
            {
                enterMethod.Invoke(PosePresenter.Instance, new object[] { enterPos, enterYRot, enterConfig });
            }
            else
            {
                PosePresenter.Instance.EnterPose(enterPos, enterYRot, enterConfig);
            }

            // Wait for black screen hold
            var delayMs = (int)(enterConfig.blackHoldDuration * 1000) + 500;
            await UniTask.Delay(delayMs);

            Debug.Log("EnterPose completed");
        }

        private async UniTask HandleFadeBlackFlashAsync()
        {
            if (ScreenFader.Instance != null)
            {
                await ScreenFader.Instance.FadeToBlackAsync(0f);
                await UniTask.Delay(1000);
                await ScreenFader.Instance.FadeFromBlackAsync(0f);
            }
        }
    }
}
