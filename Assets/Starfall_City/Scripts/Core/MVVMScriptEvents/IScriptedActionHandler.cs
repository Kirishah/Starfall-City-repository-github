using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public interface IScriptedActionHandler
    {
        ScriptedEvent.ActionCommand.Type SupportedType { get; }

        UniTask ExecuteAsync(
            ScriptedEvent.ActionCommand command,
            GameObject resolvedTarget,
            Dictionary<string, object> parameters,
            ScriptedEventExecutor executor); // executor passed for access to shared state (transfers, etc.)
    }
}
