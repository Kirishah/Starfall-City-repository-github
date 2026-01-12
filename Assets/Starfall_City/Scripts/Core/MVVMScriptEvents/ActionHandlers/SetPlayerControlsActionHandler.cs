using Movement;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class SetPlayerControlsActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.SetPlayerControls;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject _, Dictionary<string, object> __, ScriptedEventExecutor ___)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("SetPlayerControls: Player with tag 'Player' not found!");
                await UniTask.CompletedTask;
                return;
            }

            var pm = player.GetComponent<PlayerMovement>();
            var pm3d = player.GetComponent<Player3DMovement>();

            bool enable = cmd.boolValue;

            if (pm != null)
            {
                if (enable)
                    pm.ResumeControls();
                else
                    pm.PauseControls();
            }
            if (pm3d != null)
            {
                if (enable)
                    pm3d.ResumeControls();
                else
                    pm3d.PauseControls();
            }

            Debug.Log($"Player controls: {(enable ? "ENABLED" : "DISABLED")}");

            await UniTask.CompletedTask;
        }
    }
}
