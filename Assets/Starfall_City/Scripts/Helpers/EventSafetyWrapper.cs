#if UNITY_EDITOR
using UnityEngine;

public static class EventSafetyWrapper
{
    public static void SafeInvoke(this System.Action<ObjectiveSO, int, int> evt, ObjectiveSO obj, int cur, int req)
    {
        if (evt == null) return;

        var delegates = evt.GetInvocationList();
        foreach (var del in delegates)
        {
            try
            {
                ((System.Action<ObjectiveSO, int, int>)del)(obj, cur, req);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[OnObjectiveProgressed] Subscriber {del.Target} threw an exception and was REMOVED:\n{e}");
                evt -= (System.Action<ObjectiveSO, int, int>)del; // Auto-unsubscribe broken listeners
            }
        }
    }
}
#endif
