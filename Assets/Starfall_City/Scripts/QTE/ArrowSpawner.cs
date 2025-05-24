using UnityEngine;
using System.Collections;
using DanceInputActions;

public class ArrowSpawner : MonoBehaviour
{
    [SerializeField] private DanceArrowPool arrowPool;
    [SerializeField] private float beatInterval = 1.0f;

    IEnumerator SpawnArrows()
    {
        while (true)
        {
            string[] directions = { "Up", "Down", "Left", "Right" };
            string randomDir = directions[Random.Range(0, directions.Length)];

            GameObject arrowObj = arrowPool.GetArrow(randomDir);
            if (arrowObj != null)
            {
                DanceArrow arrow = arrowObj.GetComponent<DanceArrow>();
                arrow.ResetArrow();
            }

            yield return new WaitForSeconds(beatInterval);
        }
    }
}
