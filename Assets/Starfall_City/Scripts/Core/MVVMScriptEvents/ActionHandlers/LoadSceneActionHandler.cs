using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace core
{
    public class LoadSceneActionHandler : IScriptedActionHandler
    {
        private readonly ScriptedEventExecutor _executor;

        public LoadSceneActionHandler(ScriptedEventExecutor executor)
        {
            _executor = executor;
        }

        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.LoadScene;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject _, Dictionary<string, object> __, ScriptedEventExecutor ___)
        {
            if (string.IsNullOrEmpty(cmd.sceneName))
            {
                Debug.LogError("LoadScene action missing sceneName!");
                return;
            }

            if (ScreenFader.Instance != null)
                await ScreenFader.Instance.FadeToBlackAsync(1f);

            var loadMode = cmd.loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var loadOp = SceneManager.LoadSceneAsync(cmd.sceneName, loadMode);
            loadOp.allowSceneActivation = true;

            while (!loadOp.isDone)
                await UniTask.Yield();

            Debug.Log($"Scene loaded: {cmd.sceneName}");

            await _executor.ResolvePendingTransfersAsync();

            ScreenFader.Instance?.FadeFromBlackAsync(0.8f);
        }
    }
}