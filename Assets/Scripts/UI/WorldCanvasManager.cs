using UnityEngine;

public class WorldCanvasManager : MonoBehaviour
{
    public static WorldCanvasManager Instance;
    public Canvas worldCanvas; // Assign your World Canvas here

    void Awake()
    {
        if (Instance == null) Instance = this;
    }
}
