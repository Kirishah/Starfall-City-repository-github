using DialogueSystem;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class WaitForDialogueEndActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.WaitForDialogueEnd;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject _, Dictionary<string, object> __, ScriptedEventExecutor ___)
        {
            var manager = DialogueManager_UIToolkit.Instance;

            if (manager == null)
            {
                Debug.LogError("[WaitForDialogueEnd] DialogueManager_UIToolkit.Instance is null at runtime!");
                return;
            }

            bool ended = await manager.WaitForDialogueEndAsync(cmd.dialogueId);

            if (!ended)
                Debug.LogWarning($"WaitForDialogueEnd: Timed out waiting for dialogue '{cmd.dialogueId}'");
        }
    }
}
