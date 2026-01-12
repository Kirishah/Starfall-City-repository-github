using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class GameObjectExtensions
{
    public static GameObject FindWithTagIncludingInactive(this Object context, string tag)
    {
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();

        foreach (var root in rootObjects)
        {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in transforms)
            {
                if (t.CompareTag(tag))
                    return t.gameObject;
            }
        }
        return null;
    }

    public static GameObject[] FindAllWithTagIncludingInactive(this Object context, string tag)
    {
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();
        var results = new List<GameObject>();

        foreach (var root in rootObjects)
        {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var t in transforms)
            {
                if (t.CompareTag(tag))
                    results.Add(t.gameObject);
            }
        }
        return results.ToArray();
    }

    public static GameObject FindWithTagInAllScenes(this Object context, string tag)
    {
        // Fast path: active scene
        var go = GameObject.FindGameObjectWithTag(tag);
        if (go != null) return go;

        // Full search across all loaded scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.CompareTag(tag)) return root;

                var transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t.CompareTag(tag))
                        return t.gameObject;
                }
            }
        }
        return null;
    }

    public static string GetParamString(this Dictionary<string, object> paramsDict, string key, string fallback = "")
    {
        if (paramsDict != null && paramsDict.TryGetValue(key, out var val) && val is string str)
            return str;
        return fallback;
    }
}
