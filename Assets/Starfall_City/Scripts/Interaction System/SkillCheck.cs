using UnityEngine;

public static class SkillCheck
{
    public static bool PerformCheck(int skillLevel, int difficulty)
    {
        // Simple example: success if skill level is greater than or equal to difficulty
        return skillLevel >= difficulty;
    }
}
