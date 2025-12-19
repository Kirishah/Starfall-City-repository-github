using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class SetAnimActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.SetAnimationState;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> _, ScriptedEventExecutor __)
        {
            if (target != null)
            {
                var animator = target.GetComponent<Animator>();
                if (animator != null && !string.IsNullOrEmpty(cmd.paramName))
                    animator.SetBool(cmd.paramName, cmd.boolValue);
            }

            await UniTask.CompletedTask;
        }
    }
}