using core;
using System.Collections;
using TMPro;
using UnityEngine;

namespace QTE
{
    public class RivalDancer : MonoBehaviour
    {
        [Header("Config & UI")]
        [SerializeField] private QTEConfig _config;
        [SerializeField] private TextMeshProUGUI _rivalScoreText;
        [SerializeField] private TextMeshProUGUI _rivalComboText;
        [SerializeField] private PersistentReference _dancer;

        private GameObject _dancerGO;
        private Animator _dancerAnimator;

        private int _currentScore;
        private int _currentCombo;

        // Public read-only access
        public int FinalScore => _currentScore;
        public int FinalCombo => _currentCombo;

        private void Start()
        {
            if (_config == null) Debug.LogError("QTEConfig missing on RivalDancer!", this);
        }

        private void TryResolveDancer()
        {
            PersistentRegistry.OnReady -= TryResolveDancer; // Unsubscribe immediately

            if (ResolveRivalDancer())
            {
                Debug.Log("RivalDancer: Dancer resolved successfully");
            }
            else
            {
                Debug.LogError($"{name}: FAILED to resolve dancer even after registry ready!");
            }
        }

        private bool ResolveRivalDancer()
        {
            // Resolve Player Dancer
            if (_dancer == null || !_dancer.IsValid)
            {
                Debug.LogError("RivalDancer: Player dancer PersistentReference is missing or invalid!");
                return false;
            }

            _dancerGO = _dancer.Get<GameObject>();
            if (_dancerGO == null)
            {
                Debug.LogError("RivalDancer: Failed to resolve dancer GameObject — check PersistentRegistry fix!");
                return false;
            }

            _dancerAnimator = _dancerGO.GetComponent<Animator>();
            if (_dancerAnimator == null)
            {
                Debug.LogError($"RivalDancer: No Animator on dancer {_dancerGO.name}!");
                return false;
            }

            Debug.Log($"RivalDancer: Player dancer resolved → {_dancerGO.name}");
            return true;
        }

        private void OnEnable()
        {
            PersistentRegistry.OnReady += TryResolveDancer;
            DanceArrow.OnArrowEnteredHitZone += OnArrowHittable;
            DanceGameManager.OnQTEComplete += OnQTEEnded;
        }

        private void OnDisable()
        {
            PersistentRegistry.OnReady -= TryResolveDancer;
            DanceArrow.OnArrowEnteredHitZone -= OnArrowHittable;
            DanceGameManager.OnQTEComplete -= OnQTEEnded;
        }

        private void OnQTEEnded(bool _)
        {
            ResetRival();
            _dancerAnimator.SetTrigger("stop_dance");
        }

        public void ResetRival()
        {
            _currentScore = 0;
            _currentCombo = 0;
            UpdateUI();
        }

        // React to every arrow that appears (shared track)
        private void OnArrowHittable(DanceArrow arrow)
        {
            if (!_config.enableRival || !QTEGameManager.IsQTEActive) return;

            StartCoroutine(ProcessRivalReaction(arrow));
        }

        private IEnumerator ProcessRivalReaction(DanceArrow arrow)
        {
            yield return new WaitForSeconds(_config.rivalReactionDelay);

            // Only succeed if arrow still valid (not passed/missed)
            if (!arrow.gameObject.activeInHierarchy || arrow.hasPassedHitZone)
            {
                yield break; // Too late!
            }

            bool isComplex = arrow.type == DanceArrow.ArrowType.Hold || arrow.type == DanceArrow.ArrowType.Double;
            bool intentionalMiss = UnityEngine.Random.value < _config.rivalIntentionalMissChance;

            float accuracy = _config.rivalAccuracy;
            if (isComplex && intentionalMiss) accuracy *= 0.6f;

            bool success = UnityEngine.Random.value < accuracy;

            if (success)
            {
                // Animate rival dance
                _dancerAnimator.SetTrigger($"dance_{arrow.direction}");

                // Score with combo aggression
                float aggression = Mathf.Pow(_config.rivalComboAggression, _currentCombo / 10f);
                _currentCombo++;
                _currentScore += Mathf.RoundToInt(_config.basePoints * (1f + (_currentCombo * _config.comboMultiplier)) * aggression);
            }
            else
            {
                _currentCombo = 0;
                // Optional: _rivalAnimator?.SetTrigger("miss");
            }

            UpdateUI();
        }

        public void SetAIParameters(float accuracy, float reactionDelay, float intentionalMissChance, float comboAggression)
        {
            if (_config == null)
            {
                Debug.LogError("RivalDancer config is null when trying to apply AI parameters!", this);
                return;
            }

            // We mutate the serialized config directly — it's fine because it's a runtime instance
            _config.rivalAccuracy = accuracy;
            _config.rivalReactionDelay = reactionDelay;
            _config.rivalIntentionalMissChance = intentionalMissChance;
            _config.rivalComboAggression = comboAggression;

            Debug.Log($"RivalDancer AI updated → Acc:{accuracy:F2} Delay:{reactionDelay:F2}s Miss%:{intentionalMissChance:P0}");
        }

        private void UpdateUI()
        {
            if (_rivalScoreText) _rivalScoreText.text = $"Rival: {_currentScore}";
            if (_rivalComboText) _rivalComboText.text = $"x{_currentCombo}";
            if (_rivalComboText) _rivalComboText.color = Color.Lerp(Color.red, Color.magenta, _currentCombo / 15f);
        }
    }
}
