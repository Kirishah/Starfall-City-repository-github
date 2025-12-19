using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;

namespace core
{
    public class NavMeshMoveActionHandler : IScriptedActionHandler
    {
        public ScriptedEvent.ActionCommand.Type SupportedType => ScriptedEvent.ActionCommand.Type.NavMeshMove;

        public async UniTask ExecuteAsync(ScriptedEvent.ActionCommand cmd, GameObject target, Dictionary<string, object> _, ScriptedEventExecutor __)
        {
            if (target == null) return;

            var agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Debug.LogError($"NavMeshMove: No NavMeshAgent on {target.name}!");
                return;
            }

            var anim = target.GetComponent<Animator>();

            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.autoBraking = false;
            agent.isStopped = false;
            agent.ResetPath(); 

            if (anim != null)
            {
                anim.applyRootMotion = false;
                anim.updateMode = AnimatorUpdateMode.Normal;  // CRITICAL: prevents Animator from overriding position
            }

            // Ensure on NavMesh
            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(target.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
                else
                {
                    Debug.LogError($"NavMeshMove: Cannot place {target.name} on NavMesh!");
                    return;
                }
            }
            // CRITICAL FIX: Properly sample destination with multiple attempts
            Vector3 sampledDestination = cmd.targetPosition;
            bool foundValidDestination = false;

            // Try multiple sampling attempts with different radii
            float[] sampleRadii = { 1f, 2f, 5f, 10f };
            foreach (float radius in sampleRadii)
            {
                if (NavMesh.SamplePosition(cmd.targetPosition, out NavMeshHit destHit, radius, NavMesh.AllAreas))
                {
                    sampledDestination = destHit.position;
                    foundValidDestination = true;
                    Debug.Log($"NavMeshMove: Sampled destination to {sampledDestination} (radius: {radius}m)");
                    break;
                }
            }

            if (!foundValidDestination)
            {
                Debug.LogError($"NavMeshMove: Cannot find valid NavMesh position near {cmd.targetPosition}!");
                return;
            }

            // Check if destination is reachable
            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(sampledDestination, path))
            {
                Debug.LogError($"NavMeshMove: Path calculation failed for {target.name} to {sampledDestination}");
                return;
            }

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                Debug.LogError($"NavMeshMove: Path incomplete. Status: {path.status}");
                return;
            }

            // Set stopping distance
            agent.stoppingDistance = 0.2f;

            // Set destination
            bool success = agent.SetDestination(sampledDestination);
            Debug.Log($"NavMeshMove: SetDestination to {sampledDestination} = {success}");

            await UniTask.DelayFrame(2);

            float actualDistance = Vector3.Distance(agent.transform.position, sampledDestination);

            Debug.Log($"NavMeshMove: Actual distance to destination: {actualDistance:F2}m, " +
                      $"remainingDistance: {agent.remainingDistance:F2}, hasPath: {agent.hasPath}");
            string startParam = string.IsNullOrEmpty(cmd.animStartParam) ? cmd.animationName : cmd.animStartParam;
            string stopParam = string.IsNullOrEmpty(cmd.animStopParam) ? startParam : cmd.animStopParam;

            // Only skip movement if truly very close (less than ~0.8m)
            if (actualDistance <= 0.8f)
            {
                Debug.Log("NavMeshMove: Already very close to destination (<0.8m) — skipping movement but finalizing state.");

                // Still trigger stop animation if it was supposed to start
                if (anim != null)
                {
                    if (!string.IsNullOrEmpty(startParam)) anim.SetBool(startParam, false);
                    if (!string.IsNullOrEmpty(stopParam)) anim.SetBool(stopParam, false);
                }

                Debug.Log($"NavMeshMove COMPLETE: {target.name} already at destination");
                return;
            }

            // If we get here, we expect real movement
            if (!agent.hasPath || agent.pathPending)
            {
                Debug.LogWarning("NavMeshMove: No valid path after setup — forcing recalculation");
                agent.ResetPath();
                await UniTask.NextFrame();
                agent.SetDestination(sampledDestination);
                await UniTask.DelayFrame(2);
            }

            Debug.Log($"NavMeshMove: Has path: {agent.hasPath}, Path pending: {agent.pathPending}, " +
                     $"Remaining distance: {agent.remainingDistance}, Velocity: {agent.velocity.magnitude:F2}");

            // Start movement animation
            if (anim != null && !string.IsNullOrEmpty(startParam))
                anim.SetBool(startParam, true);

            // Wait for agent to actually start moving
            int waitFrames = 0;
            while (agent.velocity.magnitude < 0.1f && waitFrames < 60)
            {
                waitFrames++;
                if (waitFrames % 10 == 0)
                {
                    Debug.Log($"NavMeshMove: Frame {waitFrames}, velocity: {agent.velocity.magnitude:F2}, position: {agent.transform.position}");

                    // Force update if stuck
                    if (waitFrames >= 30)
                    {
                        Debug.Log("NavMeshMove: Force resetting destination");
                        agent.ResetPath();
                        agent.SetDestination(sampledDestination);
                    }
                }
                await UniTask.Yield();
            }
            
            Debug.Log($"NavMeshMove: After {waitFrames} frames, velocity: {agent.velocity.magnitude:F2}");
            
            if (agent.velocity.magnitude < 0.1f)
            {
                Debug.LogWarning($"NavMeshMove: Agent {target.name} failed to start moving. Attempting manual movement.");

                // Try one more time with a fresh path
                agent.ResetPath();
                await UniTask.NextFrame();
                agent.SetDestination(sampledDestination);
                await UniTask.NextFrame();

                // If still not moving, use direct movement as fallback
                if (agent.velocity.magnitude < 0.1f)
                {
                    Debug.LogWarning($"NavMeshMove: Using direct movement fallback for {target.name}");
                    await MoveDirectlyAsync(agent, sampledDestination, cmd.tolerance);
                }
            }

            // Wait for agent to reach destination
            bool reached = await agent.WaitUntilReachedAsync(sampledDestination, cmd.tolerance);

            if (!reached)
            {
                Debug.LogWarning($"NavMeshMove: WaitUntilReached returned false for {target.name}");
            }

            // Stop animation
            if (anim != null && !string.IsNullOrEmpty(stopParam))
                anim.SetBool(stopParam, false);

            Debug.Log($"NavMeshMove COMPLETE: {target.name} reached destination");
        }

        private async UniTask MoveDirectlyAsync(NavMeshAgent agent, Vector3 destination, float tolerance)
        {
            float speed = agent.speed > 0 ? agent.speed : 3.5f;
            float threshold = agent.stoppingDistance + tolerance;
            int maxFrames = 300;
            int frames = 0;

            Debug.Log($"MoveDirectlyAsync: Moving {agent.gameObject.name} directly to {destination}");

            while (frames < maxFrames)
            {
                frames++;
                Vector3 direction = (destination - agent.transform.position).normalized;
                Vector3 move = direction * speed * Time.deltaTime;

                // Use agent.Move for NavMesh-aware movement
                agent.Move(move);

                float distance = Vector3.Distance(agent.transform.position, destination);

                if (frames % 30 == 0)
                {
                    Debug.Log($"MoveDirectlyAsync: Frame {frames}, distance={distance:F2}m");
                }

                if (distance <= threshold)
                {
                    Debug.Log($"MoveDirectlyAsync: Reached destination in {frames} frames");
                    break;
                }

                await UniTask.Yield();
            }
        }
    }
}