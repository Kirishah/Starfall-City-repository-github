using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QTE;
using UnityEngine;

namespace core
{
    public class StartQTEActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.StartQTE;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject _, Dictionary<string, object> __, ScriptedEventExecutor executor)
        {
            if (QTEGameManager.Instance == null)
            {
                Debug.LogError("QTEGameManager.Instance not found!");
                await UniTask.CompletedTask;
                return;
            }

            // Create a temporary listener object that holds the callback
            var listener = new QTEListener(cmd, executor);
            DanceGameManager.OnQTEComplete += listener.OnQTEFinished;

            // Start the QTE
            string configId = string.IsNullOrEmpty(cmd.qteConfigId) ? "default" : cmd.qteConfigId;
            QTEGameManager.Instance.StartQTE(configId);

            await UniTask.CompletedTask;
        }

        // Private nested class that holds the callback logic
        private class QTEListener
        {
            private readonly ScriptedEvent.ActionCommand _cmd;
            private readonly ScriptedEventExecutor _executor;

            public QTEListener(ScriptedEvent.ActionCommand cmd, ScriptedEventExecutor executor)
            {
                _cmd = cmd;
                _executor = executor;
            }

            public void OnQTEFinished(bool success)
            {
                // Unsubscribe immediately to avoid leaks/multiple triggers
                DanceGameManager.OnQTEComplete -= OnQTEFinished;

                string nextEventId = success ? _cmd.onQTESuccessEventId : _cmd.onQTEFailureEventId;
                if (!string.IsNullOrEmpty(nextEventId))
                {
                    _ = _executor.ExecuteEventAsync(nextEventId);
                    Debug.Log($"QTE ended with {(success ? "SUCCESS" : "FAILURE")} → Triggering: {nextEventId}");
                }
            }
        }
    }
}