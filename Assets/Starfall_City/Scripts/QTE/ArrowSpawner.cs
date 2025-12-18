using UnityEngine;
using System;
using System.Linq;

namespace QTE
{
    public class ArrowSpawner : MonoBehaviour
    {
        [SerializeField] private QTEConfig config; // Centralized config
        [SerializeField] private DanceArrowPool arrowPool;

        private bool isSpawning = false;

        private double _musicStartDSP;
        private int _currentBeatIndex = 0;
        private double _nextBeatDSPTime;
        private double _avgBeatDuration;
        private const double _beatTolerance = 0.001;

        public QTEConfig Config { get => config; set => config = value; }

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

        public void StartSpawning(double musicStartDSPTime)
        {
            isSpawning = true;
            _musicStartDSP = musicStartDSPTime;
            _currentBeatIndex = 0;

            _avgBeatDuration = 60.0 / config.bpm;

            if (config.beatSpawnTimes.Count > 0)
            {
                ScheduleNextBeatmapBeat();
            }
            else
            {
                double initialDelay = config.initialSpawnDelay + config.beatOffset;
                _nextBeatDSPTime = _musicStartDSP + initialDelay;
            }

            Debug.Log($"ArrowSpawner: Beatmap mode: {(config.beatSpawnTimes.Count > 0 ? "ON (" + config.beatSpawnTimes.Count + " beats)" : "Uniform BPM")}");
        }

        private void ScheduleNextBeatmapBeat()
        {
            if (_currentBeatIndex >= config.beatSpawnTimes.Count)
            {
                _nextBeatDSPTime = double.MaxValue; 
                return;
            }

            _nextBeatDSPTime = _musicStartDSP + config.beatSpawnTimes[_currentBeatIndex];
            _currentBeatIndex++;
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        private void Update()
        {
            if (!QTEGameManager.IsQTEActive || QTEGameManager.IsQTEPaused || !isSpawning) return;

            double currentDSP = AudioSettings.dspTime;

            // Spawn arrows on every beat
            if (currentDSP + _beatTolerance >= _nextBeatDSPTime)
            {
                SpawnArrow();

                if (config.beatSpawnTimes.Count > 0)
                {
                    ScheduleNextBeatmapBeat();
                }
                else
                {
                    _nextBeatDSPTime += _avgBeatDuration;
                }
            }
        }

        private void SpawnArrow()
        {
            ArrowDirection[] directions = Enum.GetValues(typeof(ArrowDirection)).Cast<ArrowDirection>().ToArray();
            ArrowDirection randomDir = directions[UnityEngine.Random.Range(0, directions.Length)];

            // Weighted probabilities: Single (80%), Hold (5%), Double (15%)
            float rand = UnityEngine.Random.value;
            DanceArrow.ArrowType arrowType;
            if (rand < 0.80f)  // 80% Single
                arrowType = DanceArrow.ArrowType.Single;
            else if (rand < 0.85f)  // 5% Hold
                arrowType = DanceArrow.ArrowType.Hold;
            else  // 15% Double
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

                    float travelTime = (float)_avgBeatDuration * config.beatsToHitZone;
                    arrow.SetTravelTime(travelTime);
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
            _currentBeatIndex = 0;
            Debug.Log("ArrowSpawner: Reset complete", this);
        }
    } 
}
