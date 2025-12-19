using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public static class AsyncWaitExtensions
{
    // Waits asynchronously until a NavMeshAgent reaches its destination.
    public static async UniTask<bool> WaitUntilReachedAsync(
        this NavMeshAgent agent,
        Vector3 destination,
        float extraTolerance = 0f,
        int maxFrames = 600)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        float threshold = agent.stoppingDistance + extraTolerance;
        int frames = 0;
        const int maxStartupFrames = 30;

        Debug.Log($"[WaitUntilReached] Start: agent.enabled={agent.enabled}, agent.isOnNavMesh={agent.isOnNavMesh}, " +
              $"threshold={threshold:F2}m, initial dist={Vector3.Distance(agent.transform.position, destination):F2}m");

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning("[WaitUntilReached] Agent is disabled or not on NavMesh - cannot move.");
            return false;
        }

        // Phase 1: Wait for path calculation
        while (agent.pathPending && frames < maxFrames)
        {
            frames++;
            if (frames % 30 == 0) Debug.Log($"[WaitUntilReached] Path pending [{frames} frames]...");
            await UniTask.Yield();
        }

        if (agent.pathPending)
        {
            Debug.LogWarning("[WaitUntilReached] Path calculation timeout.");
            return false;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            Debug.LogError($"[WaitUntilReached] Invalid path for {agent.gameObject.name}");
            return false;
        }

        // Phase 2: Wait for agent to start moving (avoid false-positive early exit)
        Vector3 startPos = agent.transform.position;
        int startupFrames = 0;
        while (startupFrames < maxStartupFrames &&
               Vector3.Distance(agent.transform.position, startPos) < 0.01f)
        {
            startupFrames++;
            await UniTask.Yield();
        }

        // Phase 3: Wait until close enough
        frames = 0;
        float currentDist = Vector3.Distance(agent.transform.position, destination);

        Debug.Log($"[WaitUntilReached] Entering main loop: currentDist={currentDist:F2}, threshold={threshold:F2}, " +
              $"agent.enabled={agent.enabled}, agent.isOnNavMesh={agent.isOnNavMesh}, frames={frames}");

        while (agent.enabled && agent.isOnNavMesh && currentDist > threshold && frames < maxFrames)
        {
            frames++;
            currentDist = Vector3.Distance(agent.transform.position, destination);

            // Log every 30 frames or when velocity changes significantly
            if (frames % 30 == 0 || (frames < 10 && frames % 5 == 0))
            {
                Debug.Log($"[WaitUntilReached] Frame {frames}: dist={currentDist:F2}m, " +
                          $"velocity={agent.velocity.magnitude:F1}m/s, position={agent.transform.position}");
            }

            // Anti-stuck measure
            if (agent.velocity.magnitude < 0.1f && frames > 60 && frames % 30 == 0)
            {
                Debug.Log("[WaitUntilReached] Agent stuck — resetting path and destination");
                agent.ResetPath();
                agent.SetDestination(destination);
                await UniTask.Yield(); // Give it a frame to recalculate
            }

            await UniTask.Yield();
        }
        // Check why we exited the loop
        if (frames >= maxFrames)
        {
            Debug.LogWarning($"[WaitUntilReached] TIMEOUT after {frames} frames. Final dist={currentDist:F2}m");
            return false;
        }
        else if (!agent.enabled)
        {
            Debug.LogWarning("[WaitUntilReached] Agent was disabled during movement");
            return false;
        }
        else if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[WaitUntilReached] Agent left NavMesh during movement");
            return false;
        }
        else if (currentDist <= threshold)
        {
            Debug.Log($"[WaitUntilReached] SUCCESS! Reached in {frames} frames. Final dist={currentDist:F2}m");
            return true;
        }
        else
        {
            Debug.LogWarning($"[WaitUntilReached] UNEXPECTED EXIT. Frames={frames}, dist={currentDist:F2}m");
            return false;
        }
    }

    // Waits asynchronously for a specific dialogue to end (or any dialogue if expectedId is empty/null).
    public static async UniTask<bool> WaitForDialogueEndAsync(
        this DialogueManager_UIToolkit dialogueManager,
        string expectedDialogueId = null,
        int timeoutMs = 15000)
    {
        if (dialogueManager == null)
        {
            Debug.LogError("[WaitForDialogueEnd] DialogueManager instance is null!");
            return false;
        }

        var tcs = new UniTaskCompletionSource<bool>();
        Action handler = null;

        handler = () =>
        {
            string currentId = dialogueManager.currentStartID;

            bool match = string.IsNullOrEmpty(expectedDialogueId) ||
                         currentId == expectedDialogueId ||
                         string.IsNullOrEmpty(currentId);

            if (match)
            {
                DialogueManager_UIToolkit.OnDialogueEnded -= handler;
                tcs.TrySetResult(true);
                Debug.Log($"[WaitForDialogueEnd] Completed for '{expectedDialogueId ?? "<any>"}'");
            }
        };

        DialogueManager_UIToolkit.OnDialogueEnded += handler;

        // Immediate check
        if (string.IsNullOrEmpty(dialogueManager.currentStartID))
        {
            handler();
        }

        // Timeout
        UniTask.Delay(timeoutMs).ContinueWith(() =>
        {
            if (tcs.TrySetResult(false))
            {
                DialogueManager_UIToolkit.OnDialogueEnded -= handler;
                Debug.LogWarning($"[WaitForDialogueEnd] TIMED OUT waiting for '{expectedDialogueId ?? "<any>"}'");
            }
        }).Forget();

        return await tcs.Task;
    }
}
