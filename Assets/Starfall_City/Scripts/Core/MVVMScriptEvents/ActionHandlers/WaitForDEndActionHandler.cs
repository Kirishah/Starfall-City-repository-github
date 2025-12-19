using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class WaitForDialogueEndActionHandler : IScriptedActionHandler
    {
        private readonly DialogueManager_UIToolkit _dialogueManager;

        public WaitForDialogueEndActionHandler(DialogueManager_UIToolkit dialogueManager)
        {
            _dialogueManager = dialogueManager;
        }

        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.WaitForDialogueEnd;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject _, Dictionary<string, object> __, ScriptedEventExecutor ___)
        {
            bool ended = await _dialogueManager.WaitForDialogueEndAsync(cmd.dialogueId);
            if (!ended)
                Debug.LogWarning($"WaitForDialogueEnd: Timed out waiting for dialogue '{cmd.dialogueId}'");

            // No need for CompletedTask — we're already returning the awaited task
        }
    }
}