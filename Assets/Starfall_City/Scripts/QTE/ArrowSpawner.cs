using UnityEngine;
using System.Collections;
using DanceInputActions;

public class ArrowSpawner : MonoBehaviour
{
    [SerializeField] private DanceArrowPool arrowPool;
    private Coroutine spawnCoroutine;

    private void Start()
    {
        if (arrowPool == null)
        {
            Debug.LogError("Arrow Pool is not assigned in the ArrowSpawner.");
            return;
        }
        if (DanceGameManager.Instance == null)
        {
            Debug.LogError("ArrowSpawner: DanceGameManager.Instance is null!");
        }
    }

    public void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        spawnCoroutine = StartCoroutine(SpawnArrows());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    IEnumerator SpawnArrows()
    {
        yield return new WaitForSecondsRealtime(1f);
        while (true)
        {
            string[] directions = { "Up", "Down", "Left", "Right" };
            string randomDir = directions[Random.Range(0, directions.Length)];

            GameObject arrowObj = arrowPool.GetArrow(randomDir);
            Debug.Log($"Spawned arrow: {randomDir}");
            if (arrowObj != null)
            {
                DanceArrow arrow = arrowObj.GetComponent<DanceArrow>();
                if (arrow != null && DanceInput.Instance != null)
                {
                    arrow.ResetArrow();
                }
                else
                {
                    Debug.LogError($"Failed to reset arrow. DanceArrow component or DanceInput.Instance is null for direction: {randomDir}");
                }
            }
            else
            {
                Debug.LogError($"No arrow available in pool for direction: {randomDir}");
            }

            yield return new WaitForSecondsRealtime(DanceGameManager.Instance.beatInterval);
        }
    }
}
