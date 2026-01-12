using QTE;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    [RequireComponent(typeof(AudioSource))]
    public class BackgroundMusicManager : MonoBehaviour
    {
        public static BackgroundMusicManager Instance { get; private set; }

        [Header("Music Settings")]
        [SerializeField, Tooltip("List of background music clips to play in sequence.")]
        private List<AudioClip> _musicPlaylist = new();
        [SerializeField, Tooltip("Volume for background music (0 to 1).")]
        [Range(0f, 1f)] private float _musicVolume = 0.1f;
        [SerializeField, Tooltip("Fade duration when transitioning between songs (seconds).")]
        [Min(0f)] private float _fadeDuration = 1f;

        private AudioSource _musicSource;
        private int _currentTrackIndex = 0;
        private bool _isFading;
        private bool _isPausedDueToFocus; // Track if paused due to window focus
        private float _pauseTime; // Track where we paused to resume from same position
        private bool _applicationHasFocus = true; // Track application focus state

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Set up AudioSource
            _musicSource = GetComponent<AudioSource>();
            if (_musicSource == null)
            {
                Debug.LogError("BackgroundMusicManager: Failed to add AudioSource component!", this);
                enabled = false;
                return;
            }
            _musicSource.loop = false; // We'll handle looping manually
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f; // 2D audio
            _musicSource.volume = _musicVolume;

            // Validate playlist
            if (_musicPlaylist.Count == 0)
            {
                Debug.LogWarning("BackgroundMusicManager: Music playlist is empty!", this);
                enabled = false;
                return;
            }
        }

        private void SubscribeToEvents()
        {
            if (QTEGameManager.Instance != null)
            {
                QTEGameManager.OnQTEStart -= OnQTEStart; // Unsubscribe first to avoid duplicates
                QTEGameManager.OnQTEStart += OnQTEStart;
                Debug.Log("BackgroundMusicManager: Subscribed to QTEGameManager events", this);
            }
            else
            {
                Debug.LogWarning("BackgroundMusicManager: QTEGameManager.Instance is null, will try again later", this);
            }

            if (DanceGameManager.Instance != null)
            {
                DanceGameManager.OnQTEComplete -= OnQTEComplete;
                DanceGameManager.OnQTEComplete += OnQTEComplete;
                Debug.Log("BackgroundMusicManager: Subscribed to DanceGameManager events", this);
            }
            else
            {
                Debug.LogWarning("BackgroundMusicManager: DanceGameManager.Instance is null, will try again later", this);
            }
        }

        private void Start()
        {
            SubscribeToEvents();

            if (_musicPlaylist.Count > 0 && (QTEGameManager.Instance == null || !QTEGameManager.IsQTEActive))
            {
                PlayCurrentTrack();
            }
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (QTEGameManager.Instance != null)
            {
                QTEGameManager.OnQTEStart -= OnQTEStart;
                Debug.Log("BackgroundMusicManager: Unsubscribed from QTEGameManager.OnQTEStart");
            }
            if (DanceGameManager.Instance != null)
            {
                DanceGameManager.OnQTEComplete -= OnQTEComplete;
                Debug.Log("BackgroundMusicManager: Unsubscribed from DanceGameManager.OnQTEComplete");
            }
        }

        // Handle application focus changes
        private void OnApplicationFocus(bool hasFocus)
        {
            _applicationHasFocus = hasFocus;

            if (!enabled || _musicPlaylist.Count == 0 || _isFading) return;

            if (!hasFocus && _musicSource.isPlaying && (QTEGameManager.Instance == null || !QTEGameManager.IsQTEActive))
            {
                _pauseTime = _musicSource.time;
                _musicSource.Pause();
                _isPausedDueToFocus = true;
                Debug.Log("BackgroundMusicManager: Paused music due to window minimize");
            }
            else if (hasFocus && _isPausedDueToFocus && (QTEGameManager.Instance == null || !QTEGameManager.IsQTEActive))
            {
                // Wait a frame before resuming to ensure everything is properly initialized
                StartCoroutine(ResumeAfterFocusGain());
            }
        }

        private System.Collections.IEnumerator ResumeAfterFocusGain()
        {
            // Wait one frame to ensure the application is fully focused
            yield return null;

            _musicSource.time = _pauseTime;
            _musicSource.UnPause();
            _isPausedDueToFocus = false;
            Debug.Log("BackgroundMusicManager: Unpaused music after window focus gain");
        }

        private void OnApplicationPause(bool pause) =>
            // Handle mobile pause (treat as focus loss)
            OnApplicationFocus(!pause);

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Re-subscribe to QTEGameManager events to handle scene transitions
            if (QTEGameManager.Instance != null)
            {
                QTEGameManager.OnQTEStart -= OnQTEStart; // Unsubscribe first to avoid duplicates
                QTEGameManager.OnQTEStart += OnQTEStart;
            }
            if (DanceGameManager.Instance != null)
            {
                DanceGameManager.OnQTEComplete -= OnQTEComplete;
                DanceGameManager.OnQTEComplete += OnQTEComplete;
            }

            // Check QTE state and stop music if QTE is active
            if (QTEGameManager.Instance != null && QTEGameManager.IsQTEActive && _musicSource.isPlaying)
            {
                StartCoroutine(FadeOut(_musicSource, _fadeDuration));
            }
        }

        private void Update()
        {
            // Don't process music if game is paused (Time.timeScale = 0)
            if (Time.timeScale == 0f)
            {
                if (_musicSource.isPlaying)
                {
                    _musicSource.Pause();
                    Debug.Log("BackgroundMusicManager: Paused due to Time.timeScale = 0");
                }
                return;
            }
            // Only process music if the application has focus
            if (!_applicationHasFocus) return;

            if (QTEGameManager.Instance == null || !QTEGameManager.IsQTEActive)
            {
                // Only advance to next track if not paused due to focus
                if (!_musicSource.isPlaying && !_isFading && !_isPausedDueToFocus && _musicPlaylist.Count > 0 &&
                (PauseManager.Instance == null || !PauseManager.IsPaused))
                {
                    NextTrack();
                }

                // Respect pause system for non-QTE gameplay
                if (PauseManager.Instance != null && PauseManager.IsPaused && _musicSource.isPlaying)
                {
                    _musicSource.Pause();
                    _isPausedDueToFocus = false; // Ensure focus pause doesn't interfere
                    Debug.Log("BackgroundMusicManager: Paused due to PauseManager");
                }
                else if (PauseManager.Instance != null && !PauseManager.IsPaused &&
                    !_musicSource.isPlaying && !_isFading && !_isPausedDueToFocus && _musicPlaylist.Count > 0)
                {
                    _musicSource.UnPause();
                    Debug.Log("BackgroundMusicManager: Unpaused due to PauseManager");
                }
            }
            else if (_musicSource.isPlaying && !_isFading)
            {
                // QTE is active, stop music
                StartCoroutine(FadeOut(_musicSource, _fadeDuration));
            }
        }

        private void OnQTEStart()
        {
            if (_musicSource == null)
            {
                // Try to get the AudioSource if it's null
                _musicSource = GetComponent<AudioSource>();
                if (_musicSource == null)
                {
                    Debug.LogWarning("BackgroundMusicManager: musicSource is null in OnQTEStart, skipping.", this);
                    return;
                }
            }

            if (_musicSource.isPlaying)
            {
                StartCoroutine(FadeOut(_musicSource, _fadeDuration));
            }
            Debug.Log("BackgroundMusicManager: Paused for QTE", this);
        }

        private void OnQTEComplete(bool success)
        {
            if (this == null) return;  // Early exit if this instance is destroyed

            if (_musicSource == null)
            {
                // Try to get the AudioSource if it's null
                _musicSource = GetComponent<AudioSource>();
                if (_musicSource == null)
                {
                    Debug.LogWarning("BackgroundMusicManager: musicSource is null in OnQTEComplete, skipping.", this);
                    return;
                }
            }

            if (_musicPlaylist.Count > 0 && !_musicSource.isPlaying)
            {
                StartCoroutine(FadeIn(_musicSource, _fadeDuration));
                Debug.Log("BackgroundMusicManager: Resumed after QTE", this);
            }
        }

        private void PlayCurrentTrack()
        {
            if (_musicPlaylist.Count == 0)
            {
                Debug.LogWarning("BackgroundMusicManager: No tracks in playlist to play!", this);
                return;
            }

            _musicSource.clip = _musicPlaylist[_currentTrackIndex];
            _musicSource.volume = _musicVolume;
            _musicSource.Play();
            Debug.Log($"BackgroundMusicManager: Playing track {_musicPlaylist[_currentTrackIndex].name}", this);
        }

        private void NextTrack()
        {
            if (_musicPlaylist.Count == 0)
            {
                Debug.LogWarning("BackgroundMusicManager: No tracks in playlist to play!", this);
                return;
            }

            _currentTrackIndex = (_currentTrackIndex + 1) % _musicPlaylist.Count;
            PlayCurrentTrack();
        }

        private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
        {
            _isFading = true;
            float startVolume = source.volume;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
            _isFading = false;
            _isPausedDueToFocus = false; // Reset to ensure no conflict
        }

        private System.Collections.IEnumerator FadeIn(AudioSource source, float duration)
        {
            _isFading = true;
            source.clip = _musicPlaylist[_currentTrackIndex];
            source.volume = 0f;
            source.Play();

            float targetVolume = _musicVolume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(0f, targetVolume, t / duration);
                yield return null;
            }

            source.volume = targetVolume;
            _isFading = false;
        }

        public void SetVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = _musicVolume;
            Debug.Log($"BackgroundMusicManager: Volume set to {_musicVolume}", this);
        }

        public void AddTrack(AudioClip clip)
        {
            if (clip != null && !_musicPlaylist.Contains(clip))
            {
                _musicPlaylist.Add(clip);
                Debug.Log($"BackgroundMusicManager: Added track {clip.name}", this);
            }
        }

        public void RemoveTrack(AudioClip clip)
        {
            if (clip != null && _musicPlaylist.Contains(clip))
            {
                _musicPlaylist.Remove(clip);
                if (_musicSource.clip == clip)
                {
                    _musicSource.Stop();
                    NextTrack();
                }
                Debug.Log($"BackgroundMusicManager: Removed track {clip.name}", this);
            }
        }
    }
}
