using UnityEngine;

namespace Interaction
{
    [CreateAssetMenu(fileName = "New Pose Config", menuName = "Interactions/Pose Config")]
    public class PoseConfig : ScriptableObject
    {
        [Header("Pose Data")]
        public string poseID = "DefaultPose"; // e.g., "Sit", "Lie", "OpenDoor"
        public bool useRootMotion = false;

        [Header("Screen Fade")]
        [SerializeField] public float blackHoldDuration = 2f;

        [Header("Animation Triggers")]
        public string enterTrigger = "Sit"; // Animator trigger for entering pose
        public string exitTrigger = "GetUp"; // Animator trigger for exiting pose
    }
}
