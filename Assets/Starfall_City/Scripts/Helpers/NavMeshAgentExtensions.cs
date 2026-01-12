using UnityEngine;
using UnityEngine.AI;

public static class NavMeshAgentExtensions
{
    public static void EnterCinematicMode(this NavMeshAgent agent)
    {
        if (agent == null || (!agent.isOnNavMesh && !agent.isActiveAndEnabled)) return;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.velocity = Vector3.zero;
        agent.ResetPath();

        Debug.Log($"[Cinematic] NavMeshAgent paused: {agent.gameObject.name}");
    }

    public static void ExitCinematicMode(this NavMeshAgent agent)
    {
        if (agent == null || !agent.isActiveAndEnabled) return;
        if (NavMesh.SamplePosition(agent.transform.position, out var hit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        else
        {
            // No NavMesh? Stay asleep — safe forever
            agent.EnterCinematicMode();
            Debug.Log($"[Cinematic] No NavMesh → keeping agent asleep: {agent.gameObject.name}");
        }
    }
}
