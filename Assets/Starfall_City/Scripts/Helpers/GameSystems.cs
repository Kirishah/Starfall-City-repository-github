using UnityEngine;

public static class GameSystems
{
    public static bool IsReady { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() => IsReady = true;
}
