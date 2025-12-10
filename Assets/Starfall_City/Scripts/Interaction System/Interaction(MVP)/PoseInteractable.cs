using System;
using UnityEngine;

public class PoseInteractable : Interactable
{
    [Header("Pose Settings")]
    [SerializeField] private PoseConfig poseConfig; // Assign SO in Inspector (e.g., ChairSitConfig)

    [Header("Scene-Specific Position")]
    [SerializeField] private Transform targetPosition; // Assign local child Transform (e.g., Chair's "SitPoint") in scene
    [SerializeField] private Vector3 rotationOffset = Vector3.zero; // Per-instance tweak (e.g., Y=180 to face backward)

    // Optional: Fallback if no target assigned
    [SerializeField] private Vector3 positionOffset = Vector3.zero; // e.g., sit directly in front of object

    public override void Interact()
    {
        if (!_isInteractable || poseConfig == null) return;

        base.Interact(); // Handles quests, events, etc.

        // Prepare effective target (local or fallback)
        Transform effectiveTarget = targetPosition ?? transform; // Use self if no child assigned
        Vector3 effectivePos = targetPosition != null ? effectiveTarget.position : transform.position + positionOffset;

        // Invoke global presenter for transition
        PosePresenter.Instance.EnterPose(effectivePos, effectiveTarget.rotation.eulerAngles.y + rotationOffset.y, poseConfig);
    }

    public override string GetIdentifier()
    {
        return poseConfig?.poseID ?? base.GetIdentifier();
    }
}
