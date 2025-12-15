using UnityEngine;
using System.Collections;
using DanceInputActions;
using System;
using System.Linq;

namespace QTE
{
    public class ArrowSpawner : MonoBehaviour
    {
        [SerializeField] private QTEConfig config; // Centralized config
        [SerializeField] private DanceArrowPool arrowPool;
        private float spawnTimer;
        private bool isSpawning = false;
        private bool hasStartedSpawning = false;

        public QTEConfig Config
        {
            get => config;
            set => config = value;
        }

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
            hasStartedSpawning = false;
            spawnTimer = 0f;
            isSpawning = true;

            // Start a coroutine to handle the initial delay
            StartCoroutine(StartSpawningAfterDelay());
        }

        private IEnumerator StartSpawningAfterDelay()
        {
            yield return new WaitForSeconds(config.initialSpawnDelay);
            hasStartedSpawning = true;
            spawnTimer = config.beatInterval; // Start the timer so first arrow spawns immediately after delay
        }

        public void StopSpawning()
        {
            isSpawning = false;
            spawnTimer = 0f;
        }

        private void Update()
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused) return;

            if (isSpawning && !DanceInput.IsHolding && DanceGameManager.Instance != null && hasStartedSpawning)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= config.beatInterval)
                {
                    SpawnArrow();
                    spawnTimer = 0f;
                }
            }
        }
        private void SpawnArrow()
        {
            ArrowDirection[] directions = Enum.GetValues(typeof(ArrowDirection)).Cast<ArrowDirection>().ToArray();
            ArrowDirection randomDir = directions[UnityEngine.Random.Range(0, directions.Length)];

            // Weighted probabilities: Single (70%), Hold (15%), Double (15%)
            float rand = UnityEngine.Random.value;
            DanceArrow.ArrowType arrowType;
            if (rand < 0.70f)
                arrowType = DanceArrow.ArrowType.Single;
            else if (rand < 0.85f)
                arrowType = DanceArrow.ArrowType.Hold;
            else
                arrowType = DanceArrow.ArrowType.Double;

            GameObject arrowObj = arrowPool.GetArrow(randomDir);
            Debug.Log($"Spawned arrow: {randomDir}, Type: {arrowType}");
            if (arrowObj != null)
            {
                DanceArrow arrow = arrowObj.GetComponent<DanceArrow>();
                if (arrow != null && DanceInput.Instance != null)
                {
                    arrow.type = arrowType;
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
        }

        public void ResetSpawner()
        {
            StopSpawning();
            spawnTimer = 0f;
            hasStartedSpawning = false;
            StopAllCoroutines(); // Stop any running delay coroutines
            Debug.Log("ArrowSpawner: Reset complete", this);
        }
    } 
}
