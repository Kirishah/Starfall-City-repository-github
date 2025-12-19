using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class AnimationActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.PlayAnimation;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> _, ScriptedEventExecutor __)
        {
            if (target != null)
            {
                var animator = target.GetComponent<Animator>();
                if (animator != null && !string.IsNullOrEmpty(cmd.animationName))
                    animator.Play(cmd.animationName);
            }

            await UniTask.CompletedTask;
        }
    }
}