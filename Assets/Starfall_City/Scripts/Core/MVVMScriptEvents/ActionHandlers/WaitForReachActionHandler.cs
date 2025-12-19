using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace core
{
    public class WaitForReachActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.WaitForReach;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> _, ScriptedEventExecutor __)
        {
            if (target == null)
            {
                await UniTask.CompletedTask;
                return;
            }

            var agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError($"WaitForReach: No NavMeshAgent on {target.name}!");
                await UniTask.CompletedTask;
                return;
            }

            bool reached = await agent.WaitUntilReachedAsync(cmd.targetPosition, cmd.tolerance);
            if (!reached)
                Debug.LogWarning($"WaitForReach: {target.name} did not reach destination!");
        }
    }
}