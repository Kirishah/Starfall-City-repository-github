using UnityEngine;
using UnityEngine.SceneManagement;
using static Quest;

[CreateAssetMenu(menuName = "Quests/Quest")]
public class QuestSO : ScriptableObject
{
    public string QuestID;
    public string Title;
    public string Description;
    public ObjectiveSO[] Objectives;
    public Scene[] AssociatedScenes;
}
