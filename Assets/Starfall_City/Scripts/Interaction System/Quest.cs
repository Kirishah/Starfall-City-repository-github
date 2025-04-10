using System.Collections.Generic;
using UnityEngine;

public class Quest : MonoBehaviour
{
    public string questName;
    public string description;
    public bool isCompleted;
    public List<Interactable> requiredInteractables;

    public void CompleteQuest()
    {
        isCompleted = true;
        // Additional logic for quest completion
    }
}
