using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class TransferObjActionHandler : MonoBehaviour, IScriptedActionHandler
    {
        private readonly ScriptedEventExecutor _executor;

        public TransferObjActionHandler(ScriptedEventExecutor executor)
        {
            _executor = executor;
        }

        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.TransferObject;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> _, ScriptedEventExecutor __)
        {
            if (target == null || string.IsNullOrEmpty(cmd.targetSpawnId))
            {
                Debug.LogError("TransferObject: Missing target or spawnId!");
                await UniTask.CompletedTask;
                return;
            }

            DontDestroyOnLoad(target);

            if (target.GetComponent<DontDestroyOnLoadTag>() == null)
                target.AddComponent<DontDestroyOnLoadTag>();

            var pid = target.GetComponent<PersistentObjectId>() ?? target.AddComponent<PersistentObjectId>();
            pid.ForceRegisterNow();

            _executor.QueueTransfer(target, cmd.targetSpawnId, cmd.spawnYRotation);
            Debug.Log($"Queued transfer: {target.name} → spawn '{cmd.targetSpawnId}'");

            await UniTask.CompletedTask;
        }
    }
}