using System.Collections.Generic;
using UnityEngine;
using QTE;
using UnityEngine.SceneManagement;

namespace Core
{
    public class BackgroundMusicManager : MonoBehaviour
    {
        public static BackgroundMusicManager Instance { get; private set; }

        [Header("Music Settings")]
        [SerializeField, Tooltip("List of background music clips to play in sequence.")]
        private List<AudioClip> musicPlaylist = new List<AudioClip>();
        [SerializeField, Tooltip("Volume for background music (0 to 1).")]
        [Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField, Tooltip("Fade duration when transitioning between songs (seconds).")]
        [Min(0f)] private float fadeDuration = 1f;

        private AudioSource musicSource;
        private int currentTrackIndex = 0;
        private bool isFading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Set up AudioSource
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false; // We'll handle looping manually
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // 2D audio
            musicSource.volume = musicVolume;

            // Validate playlist
            if (musicPlaylist.Count == 0)
            {
                Debug.LogWarning("BackgroundMusicManager: Music playlist is empty!", this);
                enabled = false;
            }

            // Subscribe to events
            if (QTEGameManager.Instance != null)
            {
                QTEGameManager.OnQTEStart += OnQTEStart;
            }
            else
            {
                Debug.LogError("BackgroundMusicManager: QTEGameManager.Instance is null during Awake!", this);
            }
            if (DanceGameManager.Instance != null)
            {
                DanceGameManager.OnQTEComplete += OnQTEComplete;
            }
            else
            {
                Debug.LogError("BackgroundMusicManager: DanceGameManager.Instance is null during Awake!", this);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (QTEGameManager.Instance != null)
                QTEGameManager.OnQTEStart -= OnQTEStart;
            if (DanceGameManager.Instance != null)
                DanceGameManager.OnQTEComplete -= OnQTEComplete;
        }

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
            if (QTEGameManager.Instance != null && QTEGameManager.IsQTEActive && musicSource.isPlaying)
            {
                StartCoroutine(FadeOut(musicSource, fadeDuration));
            }
        }

        private void Start()
        {
            if (musicPlaylist.Count > 0 && (QTEGameManager.Instance == null || !QTEGameManager.IsQTEActive))
            {
                PlayCurrentTrack();
            }
        }

        private void Update()
        {
            if (QTEGameManager.Instance == null || !QTEGameManager.IsQTEActive)
            {
                if (!musicSource.isPlaying && !isFading && musicPlaylist.Count > 0)
                {
                    NextTrack();
                }

                // Respect pause system for non-QTE gameplay
                if (PauseManager.Instance != null && PauseManager.IsPaused && musicSource.isPlaying)
                {
                    musicSource.Pause();
                }
                else if (PauseManager.Instance != null && !PauseManager.IsPaused && !musicSource.isPlaying && musicPlaylist.Count > 0)
                {
                    musicSource.UnPause();
                }
            }
            else if (musicSource.isPlaying && !isFading)
            {
                // QTE is active, stop music
                StartCoroutine(FadeOut(musicSource, fadeDuration));
            }
        }

        private void OnQTEStart()
        {
            if (musicSource.isPlaying)
            {
                StartCoroutine(FadeOut(musicSource, fadeDuration));
            }
            Debug.Log("BackgroundMusicManager: Paused for QTE", this);
        }

        private void OnQTEComplete(bool success)
        {
            if (musicPlaylist.Count > 0 && !musicSource.isPlaying)
            {
                StartCoroutine(FadeIn(musicSource, fadeDuration));
                Debug.Log("BackgroundMusicManager: Resumed after QTE", this);
            }
        }

        private void PlayCurrentTrack()
        {
            if (musicPlaylist.Count == 0)
            {
                Debug.LogWarning("BackgroundMusicManager: No tracks in playlist to play!", this);
                return;
            }

            musicSource.clip = musicPlaylist[currentTrackIndex];
            musicSource.volume = musicVolume;
            musicSource.Play();
            Debug.Log($"BackgroundMusicManager: Playing track {musicPlaylist[currentTrackIndex].name}", this);
        }

        private void NextTrack()
        {
            if (musicPlaylist.Count == 0)
            {
                Debug.LogWarning("BackgroundMusicManager: No tracks in playlist to play!", this);
                return;
            }

            currentTrackIndex = (currentTrackIndex + 1) % musicPlaylist.Count;
            PlayCurrentTrack();
        }

        private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
        {
            isFading = true;
            float startVolume = source.volume;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
            isFading = false;
        }

        private System.Collections.IEnumerator FadeIn(AudioSource source, float duration)
        {
            isFading = true;
            source.clip = musicPlaylist[currentTrackIndex];
            source.volume = 0f;
            source.Play();

            float targetVolume = musicVolume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                source.volume = Mathf.Lerp(0f, targetVolume, t / duration);
                yield return null;
            }

            source.volume = targetVolume;
            isFading = false;
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume;
            Debug.Log($"BackgroundMusicManager: Volume set to {musicVolume}", this);
        }

        public void AddTrack(AudioClip clip)
        {
            if (clip != null && !musicPlaylist.Contains(clip))
            {
                musicPlaylist.Add(clip);
                Debug.Log($"BackgroundMusicManager: Added track {clip.name}", this);
            }
        }

        public void RemoveTrack(AudioClip clip)
        {
            if (clip != null && musicPlaylist.Contains(clip))
            {
                musicPlaylist.Remove(clip);
                if (musicSource.clip == clip)
                {
                    musicSource.Stop();
                    NextTrack();
                }
                Debug.Log($"BackgroundMusicManager: Removed track {clip.name}", this);
            }
        }
    }
}
