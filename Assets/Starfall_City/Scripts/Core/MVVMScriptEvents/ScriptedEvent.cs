using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ScriptedEvent", menuName = "Game/ScriptedEvent")]
public class ScriptedEvent : ScriptableObject
{
    [System.Serializable]
    public class ActionCommand
    {
        public enum Type
        {
            Activate, Deactivate, Reposition, PlayAnimation, Custom,
            NavMeshMove, StartDialogue, SetPlayerControls, SetAnimationState, WaitForReach
        }
        public Type actionType;
        public string targetTag;
        public Vector3 targetPosition; // For Reposition/NavMeshMove
        public string animationName; // For PlayAnimation
        public string paramName; // For SetAnimationState
        public bool boolValue; // For SetAnimationState/SetPlayerControls
        public string dialogueId; // For StartDialogue
        public string speaker; // For StartDialogue
        public float delaySeconds = 0f;
        public bool parallel = false;
        public float tolerance = 0.2f; // For WaitForReach/NavMeshMove stopping checks
    }

    public string eventId; // e.g., "IntroStart", "PostDialogueIntro"
    public List<ActionCommand> actions = new List<ActionCommand>();
    public string triggerCondition; // e.g., "SceneLoaded:Office" or "DialogueEnded:Intro"
}
