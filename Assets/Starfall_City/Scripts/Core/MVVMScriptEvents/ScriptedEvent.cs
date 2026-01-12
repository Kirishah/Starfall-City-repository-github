using UnityEngine;
using System.Collections.Generic;
using Interaction;

namespace core
{
    [CreateAssetMenu(fileName = "ScriptedEvent", menuName = "Game/ScriptedEvent")]
    public class ScriptedEvent : ScriptableObject
    {
        [System.Serializable]
        public class ActionCommand
        {
            public enum CustomType
            {
                None, RotateToFace, ExitPose, EnterPose, FadeBlackFlash, FindClosestObject // Extensible: Add more here
            }

            public enum Type
            {
                Activate, Deactivate, Reposition, PlayAnimation, Custom,
                NavMeshMove, StartDialogue, SetPlayerControls, SetAnimationState, WaitForDialogueEnd, WaitForReach,
                LoadScene, StartQTE, TransferObject
            }

            [Tooltip("The primary type of action this command performs. Why: Routes execution to the appropriate logic in the switch statement, enabling a wide range of behaviors from a unified system. Example: NavMeshMove for AI pathfinding to a desk, or StartDialogue for narrative triggers.")]
            public Type actionType;

            [Tooltip("The Unity tag of the GameObject to apply this action to. Why: Allows dynamic runtime discovery of targets without hard references, promoting flexibility in scene layouts. Leave empty for actions that don't target objects (e.g., StartDialogue). Example: 'Boss' to move an NPC, or 'Player' for control toggles.")]
            public string targetTag;

            [Tooltip("The 3D world position to use for this action. Why: Provides a fixed destination for movement or placement; can be overridden at runtime via positionParamKey for dynamic scenarios. Example: (0, 0, 5) for repositioning to a desk location.")]
            public Vector3 targetPosition; // For Reposition/NavMeshMove

            [Tooltip("The name of the specific animation clip or state to trigger. Why: Integrates with Unity's Animator for visual feedback; also serves as a fallback for parameter names in movement actions. Example: 'Idle' to play a resting animation on an NPC.")]
            public string animationName; // For PlayAnimation

            [Tooltip("The name of the boolean parameter in the Animator to modify. Why: Allows fine-grained control over animation states without playing full clips. Used primarily in SetAnimationState. Example: 'isCrouching' to toggle a stealth mode.")]
            public string paramName; // For SetAnimationState

            [Tooltip("The boolean value to assign to parameters or toggles in this action. Why: Enables on/off states for controls, animations, or flags; keeps actions simple and binary. Example: True to enable player input during free roam.")]
            public bool boolValue; // For SetAnimationState/SetPlayerControls

            [Tooltip("The unique ID of the dialogue sequence to begin. Why: Links actions to narrative content in the DialogueManager; ensures scripted events drive story progression. Example: 'BossConfrontation' for a key intro conversation.")]
            public string dialogueId; // For StartDialogue

            [Tooltip("The display name or label for the current speaker in the dialogue UI. Why: Provides context for subtitles/portraits; enhances immersion without code. Example: 'Dr. Evil' to show the boss's avatar and name.")]
            public string speaker; // For StartDialogue

            [Tooltip("The time delay (in seconds) before this action begins executing. Why: Enables precise timing and sequencing in event flows, like staggered entrances. Default: 0 (immediate). Example: 3.0f to pause before starting a dialogue after movement.")]
            public float delaySeconds = 0f;

            [Tooltip("If true, this action runs concurrently with others in the event (via Task.WhenAll). Why: Supports complex, overlapping behaviors like simultaneous animations and sounds. Default: false (sequential). Example: True for a background fade while the boss walks.")]
            public bool parallel = false;

            [Tooltip("The acceptable distance threshold for 'reaching' a target position. Why: Accounts for NavMesh imprecision and avoids infinite waits; adjustable per-action. Default: 0.2f. Example: 1.0f for loose stopping in open areas.")]
            public float tolerance = 0.2f; // For WaitForReach/NavMeshMove stopping checks

            [Tooltip("The dictionary key from runtime parameters to override targetPosition dynamically. Why: Allows events to adapt to scene-specific data (e.g., computed positions) without asset edits. Leave empty for fixed positions. Example: 'deskPos' to use a variable desk location passed from IntroManager.")]
            public string positionParamKey = ""; // e.g., "deskPos" for runtime override

            [Tooltip("The PoseConfig asset to use for EnterPose/ExitPose actions. Why: Provides the full config (triggers, durations) directly in the action; can be overridden at runtime via params['poseConfig']. Example: ChairSitConfig for sitting at desks.")]
            public PoseConfig poseConfig;  // NEW: Direct SO reference for poses

            [Tooltip("The dictionary key from runtime parameters to override targetYRotation dynamically for EnterPose. Why: Allows adaptive rotations (e.g., face-away from desk). Leave empty for fixed targetPosition's Y. Example: 'deskFacing' to use a computed angle.")]
            public string rotationParamKey = "";  // NEW: For EnterPose dynamic Y-rot

            [Tooltip("The name of the method to invoke on PosePresenter.Instance for EnterPose actions. Why: Allows configurable enter behaviors via reflection (e.g., 'EnterPoseWithSound'). Default: 'EnterPose'.")]
            public string enterMethodName = "EnterPose";  // NEW: Mirror of exitMethodName

            [Tooltip("The specific subtype of custom behavior to execute when actionType is Custom. Why: Extends the system data-driven without new enums or code; keeps custom logic centralized. Default: None. Example: RotateToFace to make an NPC look at the player.")]
            public CustomType customSubType = CustomType.None; // For Custom actions

            [Tooltip("The Animator boolean parameter to set true at the start of NavMeshMove (e.g., for walking). Why: Synchronizes animations with movement for polished AI. Leave empty to skip. Falls back to animationName if unset. Example: 'isMoving' to trigger footstep sounds.")]
            public string animStartParam = ""; // e.g., "isWalking" for NavMeshMove start

            [Tooltip("The Animator boolean parameter to set false at the end of NavMeshMove (e.g., for stopping). Why: Resets movement states cleanly post-arrival. Defaults to animStartParam if empty. Example: 'isMoving' to blend back to idle.")]
            public string animStopParam = ""; // Optional; defaults to animStartParam if empty

            [Tooltip("The fallback tag for the GameObject to apply ExitPose to (if targetTag is empty). Why: Ensures pose actions target the right entity even in generic setups. Default: 'Player'. Example: 'NPC' for character-specific exits.")]
            public string poseTargetTag = "Player"; // For ExitPose fallback

            [Tooltip("The name of the method to invoke on PosePresenter.Instance for ExitPose actions. Why: Allows configurable exit behaviors via reflection, promoting reuse. Default: 'ExitPose'. Example: 'SmoothExit' for a custom fade-out method.")]
            public string exitMethodName = "ExitPose"; // For reflection if needed; default to instance method

            [Tooltip("For LoadScene: The name of the scene to load (must be in Build Settings).")]
            public string sceneName;

            [Tooltip("For LoadScene: Whether to load additively or single (default: Single).")]
            public bool loadAdditively = false;

            [Tooltip("For StartQTE: The ID of the QTE configuration to use. If empty, uses the default config in QTEGameManager.")]
            public string qteConfigId = "";

            [Tooltip("For StartQTE: Optional success/failure event IDs to trigger after QTE ends.")]
            public string onQTESuccessEventId;
            public string onQTEFailureEventId;

            [Tooltip("Spawn ID in the new scene where this object should appear")]
            public string targetSpawnId;

            [Tooltip("Optional: Rotation override (Euler Y). Leave 0 to keep current.")]
            public float spawnYRotation = 0f;
        }

        [Tooltip("A unique string ID for this entire scripted event. Why: Used by the EventBus for triggering and by GameStateModel to prevent re-execution; ensures idempotency. Example: 'IntroStart' for the opening boss approach sequence.")]
        public string eventId; // e.g., "IntroStart", "PostDialogueIntro"

        [Tooltip("The ordered list of ActionCommands that define this event's behavior. Why: Composes complex sequences from simple, reusable actions; supports delays and parallelism for rich interactions. Example: Move boss, wait for reach, then start dialogue.")]
        public List<ActionCommand> actions = new();

        [Tooltip("An optional string condition for external or legacy triggering of this event. Why: Provides a manual hook for non-EventBus uses; primarily managed via EventSubscriptionConfig for modernity. Example: 'ManualTrigger:Debug' for editor testing.")]
        public string triggerCondition; // e.g., "SceneLoaded:Office" or "DialogueEnded:Intro"
    }
}
