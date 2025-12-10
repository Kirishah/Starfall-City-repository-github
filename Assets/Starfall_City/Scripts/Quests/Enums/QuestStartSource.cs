public enum QuestStartSource
{
    Manual_Choice,    // e.g., dialogue action
    Auto_DialogueEnd, // e.g., EndDialogue auto-start
    FollowUp,         // e.g., CheckFollowUps
    Event,            // e.g., external trigger (NPC, item)
    Unknown           // Default for legacy calls
}
