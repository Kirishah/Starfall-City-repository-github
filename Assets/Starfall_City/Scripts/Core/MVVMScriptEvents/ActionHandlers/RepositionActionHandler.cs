using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class RepositionActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.Reposition;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> @params, ScriptedEventExecutor _)
        {
            if (target == null)
            {
                await UniTask.CompletedTask;
                return;
            }

            Vector3 pos = cmd.targetPosition;

            if (!string.IsNullOrEmpty(cmd.positionParamKey) && @params != null && @params.TryGetValue(cmd.positionParamKey, out object posObj) && posObj is Vector3 v1)
                pos = v1;
            else if (@params != null && @params.TryGetValue("foundPosition", out object foundPos) && foundPos is Vector3 v2)
                pos = v2;

            target.transform.position = pos;

            await UniTask.CompletedTask;
        }
    }
}