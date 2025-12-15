using core;
using System.Collections;
using TMPro;
using UnityEngine;

namespace QTE
{
    public class RivalDancer : MonoBehaviour
    {
        [Header("Config & UI")]
        [SerializeField] private QTEConfig config;
        [SerializeField] private TextMeshProUGUI rivalScoreText;
        [SerializeField] private TextMeshProUGUI rivalComboText;
        [SerializeField] private PersistentReference dancer;

        private GameObject _dancerGO;
        private Animator _dancerAnimator;

        private int currentScore;
        private int currentCombo;

        // Public read-only access
        public int FinalScore => currentScore;
        public int FinalCombo => currentCombo;

        private void Start()
        {
            if (config == null) Debug.LogError("QTEConfig missing on RivalDancer!", this);
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
            if (dancer == null || !dancer.IsValid)
            {
                Debug.LogError("RivalDancer: Player dancer PersistentReference is missing or invalid!");
                return false;
            }

            _dancerGO = dancer.Get<GameObject>();
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
            _dancerAnimator?.SetTrigger("stop_dance");
        }

        public void ResetRival()
        {
            currentScore = 0;
            currentCombo = 0;
            UpdateUI();
        }

        // React to every arrow that appears (shared track)
        private void OnArrowHittable(DanceArrow arrow)
        {
            if (!config.enableRival || !QTEGameManager.IsQTEActive) return;

            StartCoroutine(ProcessRivalReaction(arrow));
        }

        private IEnumerator ProcessRivalReaction(DanceArrow arrow)
        {
            yield return new WaitForSeconds(config.rivalReactionDelay);

            // Only succeed if arrow still valid (not passed/missed)
            if (!arrow.gameObject.activeInHierarchy || arrow.hasPassedHitZone)
            {
                yield break; // Too late!
            }

            bool isComplex = arrow.type == DanceArrow.ArrowType.Hold || arrow.type == DanceArrow.ArrowType.Double;
            bool intentionalMiss = UnityEngine.Random.value < config.rivalIntentionalMissChance;

            float accuracy = config.rivalAccuracy;
            if (isComplex && intentionalMiss) accuracy *= 0.6f;

            bool success = UnityEngine.Random.value < accuracy;

            if (success)
            {
                // Animate rival dance
                _dancerAnimator?.SetTrigger($"dance_{arrow.direction}");

                // Score with combo aggression
                float aggression = Mathf.Pow(config.rivalComboAggression, currentCombo / 10f);
                currentCombo++;
                currentScore += Mathf.RoundToInt(config.basePoints * (1f + currentCombo * config.comboMultiplier) * aggression);
            }
            else
            {
                currentCombo = 0;
                // Optional: _rivalAnimator?.SetTrigger("miss");
            }

            UpdateUI();
        }

        public void SetAIParameters(float accuracy, float reactionDelay, float intentionalMissChance, float comboAggression)
        {
            if (config == null)
            {
                Debug.LogError("RivalDancer config is null when trying to apply AI parameters!", this);
                return;
            }

            // We mutate the serialized config directly — it's fine because it's a runtime instance
            config.rivalAccuracy = accuracy;
            config.rivalReactionDelay = reactionDelay;
            config.rivalIntentionalMissChance = intentionalMissChance;
            config.rivalComboAggression = comboAggression;

            Debug.Log($"RivalDancer AI updated → Acc:{accuracy:F2} Delay:{reactionDelay:F2}s Miss%:{intentionalMissChance:P0}");
        }

        private void UpdateUI()
        {
            if (rivalScoreText) rivalScoreText.text = $"Rival: {currentScore}";
            if (rivalComboText) rivalComboText.text = $"x{currentCombo}";
            if (rivalComboText) rivalComboText.color = Color.Lerp(Color.red, Color.magenta, currentCombo / 15f);
        }
    } 
}
