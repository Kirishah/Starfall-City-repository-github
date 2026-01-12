using DialogueSystem;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class StartDialogueActionHandler : IScriptedActionHandler
    {
        private readonly DialogueManager_UIToolkit _dialogueManager;

        public StartDialogueActionHandler(DialogueManager_UIToolkit dialogueManager)
        {
            _dialogueManager = dialogueManager;
        }

        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.StartDialogue;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject _, Dictionary<string, object> __, ScriptedEventExecutor ___)
        {
            if (_dialogueManager != null && !string.IsNullOrEmpty(cmd.dialogueId))
                _dialogueManager.StartDialogue(cmd.dialogueId, cmd.speaker);

            await UniTask.CompletedTask;
        }
    }
}
