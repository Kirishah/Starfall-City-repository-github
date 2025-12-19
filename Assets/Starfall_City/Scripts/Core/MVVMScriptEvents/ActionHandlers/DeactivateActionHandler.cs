using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class DeactivateActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.Deactivate;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> @params, ScriptedEventExecutor _)
        {
            GameObject toDeactivate = target;

            if (toDeactivate == null && @params != null && @params.TryGetValue("objectToHide", out object obj) && obj is GameObject go)
                toDeactivate = go;

            if (toDeactivate != null)
            {
                toDeactivate.SetActive(false);
                Debug.Log($"Deactivated: {toDeactivate.name}");
            }

            await UniTask.CompletedTask;
        }
    }
}