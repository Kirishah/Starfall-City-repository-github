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
                    await HandleExitPose(cmd, _executor);
                    break;

                case ScriptedEvent.ActionCommand.CustomType.EnterPose:
                    await HandleEnterPose(cmd, @params, _executor);
                    break;

                case ScriptedEvent.ActionCommand.CustomType.FadeBlackFlash:
                    await HandleFadeBlackFlash();
                    break;

                default:
                    Debug.LogWarning($"Unknown CustomType: {cmd.customSubType}");
                    break;
            }
        }

        private void HandleFindClosestObject(ScriptedEvent.ActionCommand cmd, Dictionary<string, object> @params)
        {
            string hideTag = @params.GetParamString("hideTag");
            string refTag = @params.GetParamString("referenceTag", "Player");

            if (string.IsNullOrEmpty(hideTag)) return;

            GameObject reference = GameObject.FindGameObjectWithTag(refTag) ?? GameObject.FindWithTag("Player");
            if (reference == null) return;

            GameObject closest = null;
            float bestDist = float.MaxValue;

            var allWithTag = this.FindAllWithTagIncludingInactive(hideTag); // using your extension
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
                @params["objectToHide"] = closest;
                Debug.Log($"[FindClosestObject] Found: {closest.name}");
            }
        }

        private void HandleRotateToFace(ScriptedEvent.ActionCommand cmd, GameObject target)
        {
            string lookAtTag = string.IsNullOrEmpty(cmd.paramName) ? "Player" : cmd.paramName;
            var lookAt = GameObject.FindGameObjectWithTag(lookAtTag);
            if (lookAt != null && target != null)
            {
                Vector3 dir = lookAt.transform.position - target.transform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                    target.transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        private async UniTask HandleExitPose(ScriptedEvent.ActionCommand cmd, ScriptedEventExecutor executor)
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
                PoseConfig config = playerAnim?.CurrentConfig;
                PosePresenter.Instance.ExitPose(config);
            }

            if (playerAnim != null)
                await executor.AwaitCoroutineAsync(playerAnim.WaitForPoseComplete(false));
            else
                await UniTask.Delay(2000);
        }

        private async UniTask HandleEnterPose(ScriptedEvent.ActionCommand cmd, Dictionary<string, object> @params, ScriptedEventExecutor executor)
        {
            var player = string.IsNullOrEmpty(cmd.poseTargetTag)
                ? GameObject.FindGameObjectWithTag("Player")
                : GameObject.FindGameObjectWithTag(cmd.poseTargetTag);

            if (PosePresenter.Instance == null || player == null)
            {
                Debug.LogWarning("EnterPose: Missing presenter or player!");
                return;
            }

            PoseConfig enterConfig = cmd.poseConfig;

            string poseID = @params.GetParamString("poseID");
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
            else if (enterConfig == null && @params != null && @params.TryGetValue("poseConfig", out object configObj) && configObj is PoseConfig pc)
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
            if (!string.IsNullOrEmpty(cmd.positionParamKey) && @params != null && @params.TryGetValue(cmd.positionParamKey, out object posObj) && posObj is Vector3 vPos)
            {
                enterPos = vPos;
            }
            else if (@params != null && @params.TryGetValue("foundPosition", out object foundPos) && foundPos is Vector3 fPos)
            {
                enterPos = fPos;
            }

            // Resolve rotation Y
            float enterYRot = 0f;
            if (@params != null && @params.TryGetValue("foundRotationY", out object rotObj) && rotObj is float fRot)
            {
                enterYRot = fRot;
            }
            else if (!string.IsNullOrEmpty(cmd.rotationParamKey) && @params != null && @params.TryGetValue(cmd.rotationParamKey, out object rObj) && rObj is float rotVal)
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
            int delayMs = (int)(enterConfig.blackHoldDuration * 1000) + 500;
            await UniTask.Delay(delayMs);

            Debug.Log("EnterPose completed");
        }

        private async UniTask HandleFadeBlackFlash()
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
