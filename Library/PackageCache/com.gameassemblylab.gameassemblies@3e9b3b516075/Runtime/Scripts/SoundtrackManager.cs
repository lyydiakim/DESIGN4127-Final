using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SoundtrackManager : MonoBehaviour
{
    [System.Serializable]
    public class SoundtrackEvent : UnityEvent<string> { }

    public static SoundtrackManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource primarySource;
    [SerializeField] private AudioSource secondarySource;
    [SerializeField] private float crossfadeDuration = 2.0f;

    [Header("Soundtracks")]
    [SerializeField] private List<AudioClip> levelSoundtracks = new List<AudioClip>();
    [SerializeField] private AudioClip mainMenuSoundtrack;
    [SerializeField] private AudioClip levelCompleteSoundtrack;

    [Header("Settings")]
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loopSoundtracks = true;
    [SerializeField] private float defaultVolume = 0.7f;
    [SerializeField] private bool randomizePlaylist = false;

    // Events
    public SoundtrackEvent OnSoundtrackChanged = new SoundtrackEvent();
    public UnityEvent OnSoundtrackCycleCompleted = new UnityEvent();

    // Private variables
    private int currentTrackIndex = -1;
    private bool isPlaying = false;
    private bool isCrossfading = false;
    private Coroutine fadeCoroutine;
    private Coroutine playlistCoroutine;
    private List<int> playlistOrder = new List<int>();
    private SoundtrackState currentState = SoundtrackState.Stopped;

    private enum SoundtrackState
    {
        Stopped,
        Playing,
        Paused
    }

    private void Awake()
    {   
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
            InitializeSoundtrackManager();
        } else
        {
            Destroy(gameObject);
        }
        
    }

    private void InitializeSoundtrackManager()
    {
        // Create audio sources if not assigned
        if (primarySource == null)
        {
            primarySource = gameObject.AddComponent<AudioSource>();
        }

        if (secondarySource == null)
        {
            secondarySource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio sources
        primarySource.playOnAwake = false;
        primarySource.loop = false;
        primarySource.volume = defaultVolume;

        secondarySource.playOnAwake = false;
        secondarySource.loop = false;
        secondarySource.volume = 0f;

        // Generate initial playlist order
        GeneratePlaylistOrder();

        // Start playing if set to play on awake
        if (playOnAwake && levelSoundtracks.Count > 0)
        {
            PlayLevelSoundtracks();
        }
    }

    private void OnEnable()
    {
        // Subscribe to events from other managers if needed
        // Example: GameManager.OnGameStateChanged.AddListener(HandleGameStateChanged);
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        // Example: GameManager.OnGameStateChanged.RemoveListener(HandleGameStateChanged);
    }

    public void PlayLevelSoundtracks()
    {
        if (levelSoundtracks.Count == 0)
        {
            Debug.LogWarning("No level soundtracks assigned to SoundtrackManager");
            return;
        }

        StopAllCoroutines();
        currentState = SoundtrackState.Playing;
        isPlaying = true;

        playlistCoroutine = StartCoroutine(PlaySoundtrackSequence());
    }

    public void PlayMainMenuSoundtrack()
    {
        if (mainMenuSoundtrack == null)
        {
            Debug.LogWarning("No main menu soundtrack assigned to SoundtrackManager");
            return;
        }

        StopAllCoroutines();
        CrossfadeToTrack(mainMenuSoundtrack, true);
        currentState = SoundtrackState.Playing;
        isPlaying = true;
    }

    public void PlayLevelCompleteSoundtrack()
    {
        if (levelCompleteSoundtrack == null)
        {
            Debug.LogWarning("No level complete soundtrack assigned to SoundtrackManager");
            return;
        }

        StopAllCoroutines();
        CrossfadeToTrack(levelCompleteSoundtrack, false);
        currentState = SoundtrackState.Playing;
        isPlaying = true;
    }

    public void PauseCurrentSoundtrack()
    {
        if (currentState == SoundtrackState.Playing)
        {
            primarySource.Pause();
            secondarySource.Pause();
            currentState = SoundtrackState.Paused;

            if (playlistCoroutine != null)
            {
                StopCoroutine(playlistCoroutine);
            }
        }
    }

    public void ResumeCurrentSoundtrack()
    {
        if (currentState == SoundtrackState.Paused)
        {
            primarySource.UnPause();
            secondarySource.UnPause();
            currentState = SoundtrackState.Playing;

            if (playlistCoroutine == null && loopSoundtracks)
            {
                playlistCoroutine = StartCoroutine(PlaySoundtrackSequence());
            }
        }
    }

    public void PlaySpecificSoundtrack(int soundtrackIndex)
    {
        if (soundtrackIndex < 0 || soundtrackIndex >= levelSoundtracks.Count)
        {
            Debug.LogWarning("Invalid soundtrack index: " + soundtrackIndex);
            return;
        }

        StopAllCoroutines();
        currentState = SoundtrackState.Playing;
        isPlaying = true;

        // Update the current track index to match the selected soundtrack
        // Find the position of the soundtrackIndex in the playlistOrder
        for (int i = 0; i < playlistOrder.Count; i++)
        {
            if (playlistOrder[i] == soundtrackIndex)
            {
                currentTrackIndex = i;
                break;
            }
        }

        // Use CrossfadeToTrack instead of PlayTrack for smooth transition
        CrossfadeToTrack(levelSoundtracks[soundtrackIndex], loopSoundtracks);

        // If looping is enabled, start the playlist sequence from this track
        if (loopSoundtracks)
        {
            if (playlistCoroutine != null)
            {
                StopCoroutine(playlistCoroutine);
            }
            playlistCoroutine = StartCoroutine(PlaySoundtrackSequence());
        }
    }

    public void StopCurrentSoundtrack()
    {
        StopAllCoroutines();
        fadeCoroutine = StartCoroutine(FadeOut(primarySource, 1.0f));
        fadeCoroutine = StartCoroutine(FadeOut(secondarySource, 1.0f));
        currentState = SoundtrackState.Stopped;
        isPlaying = false;
        playlistCoroutine = null;
    }

    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        defaultVolume = volume;

        // Only adjust the active source's volume
        if (!isCrossfading)
        {
            if (primarySource.isPlaying)
            {
                primarySource.volume = volume;
            } else if (secondarySource.isPlaying)
            {
                secondarySource.volume = volume;
            }
        }
    }

    public void ShufflePlaylist()
    {
        randomizePlaylist = true;
        GeneratePlaylistOrder();
    }

    public void OrderedPlaylist()
    {
        randomizePlaylist = false;
        GeneratePlaylistOrder();
    }

    public void SkipToNextTrack()
    {
        if (levelSoundtracks.Count > 1)
        {
            StopAllCoroutines();
            currentTrackIndex = GetNextTrackIndex();
            CrossfadeToTrack(levelSoundtracks[playlistOrder[currentTrackIndex]], false);
            playlistCoroutine = StartCoroutine(PlaySoundtrackSequence());
        }
    }

    public void SkipToPreviousTrack()
    {
        if (levelSoundtracks.Count > 1)
        {
            StopAllCoroutines();
            currentTrackIndex = GetPreviousTrackIndex();
            CrossfadeToTrack(levelSoundtracks[playlistOrder[currentTrackIndex]], false);
            playlistCoroutine = StartCoroutine(PlaySoundtrackSequence());
        }
    }

    private IEnumerator PlaySoundtrackSequence()
    {
        // If we're starting fresh, play the first track
        if (currentTrackIndex == -1)
        {
            currentTrackIndex = 0;
            PlayTrack(levelSoundtracks[playlistOrder[currentTrackIndex]]);
        }

        while (isPlaying && loopSoundtracks)
        {
            // Wait until the current track is almost finished
            AudioSource currentSource = primarySource.isPlaying ? primarySource : secondarySource;
            float waitTime = currentSource.clip.length - currentSource.time - crossfadeDuration;

            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            // Move to the next track
            currentTrackIndex = GetNextTrackIndex();

            // If we've completed a full cycle, invoke the event
            if (currentTrackIndex == 0)
            {
                OnSoundtrackCycleCompleted.Invoke();

                // If we need to regenerate the playlist order on each cycle
                if (randomizePlaylist)
                {
                    GeneratePlaylistOrder();
                }
            }

            // Start the next track with crossfade
            CrossfadeToTrack(levelSoundtracks[playlistOrder[currentTrackIndex]], loopSoundtracks);

            // Wait for crossfade to complete
            yield return new WaitForSeconds(crossfadeDuration + 0.1f);
        }
    }

    private void CrossfadeToTrack(AudioClip clip, bool loop)
    {
        if (clip == null) return;

        AudioSource sourceToFadeIn;
        AudioSource sourceToFadeOut;

        // Determine which source to use for the new track
        if (primarySource.isPlaying)
        {
            sourceToFadeIn = secondarySource;
            sourceToFadeOut = primarySource;
        } else
        {
            sourceToFadeIn = primarySource;
            sourceToFadeOut = secondarySource;
        }

        // Setup the new track
        sourceToFadeIn.clip = clip;
        sourceToFadeIn.loop = loop;
        sourceToFadeIn.volume = 0f;
        sourceToFadeIn.Play();

        // Perform the crossfade
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(Crossfade(sourceToFadeOut, sourceToFadeIn));

        // Notify listeners about the track change
        OnSoundtrackChanged.Invoke(clip.name);
    }

    private void PlayTrack(AudioClip clip)
    {
        if (clip == null) return;

        primarySource.clip = clip;
        primarySource.loop = loopSoundtracks && levelSoundtracks.Count == 1;
        primarySource.volume = defaultVolume;
        primarySource.Play();

        // Notify listeners about the track change
        OnSoundtrackChanged.Invoke(clip.name);
    }

    private IEnumerator Crossfade(AudioSource sourceToFadeOut, AudioSource sourceToFadeIn)
    {
        isCrossfading = true;
        float timeElapsed = 0;
        float startVolume = sourceToFadeOut.volume;

        while (timeElapsed < crossfadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / crossfadeDuration;
            sourceToFadeOut.volume = Mathf.Lerp(startVolume, 0, t);
            sourceToFadeIn.volume = Mathf.Lerp(0, defaultVolume, t);
            yield return null;
        }

        sourceToFadeOut.Stop();
        sourceToFadeOut.volume = 0;
        sourceToFadeIn.volume = defaultVolume;
        isCrossfading = false;
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / duration;
            source.volume = Mathf.Lerp(startVolume, 0, t);
            yield return null;
        }

        source.Stop();
        source.volume = 0;
    }

    private void GeneratePlaylistOrder()
    {
        playlistOrder.Clear();

        // Add all tracks to the playlist
        for (int i = 0; i < levelSoundtracks.Count; i++)
        {
            playlistOrder.Add(i);
        }

        // Shuffle the playlist if randomized
        if (randomizePlaylist)
        {
            for (int i = playlistOrder.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int temp = playlistOrder[i];
                playlistOrder[i] = playlistOrder[j];
                playlistOrder[j] = temp;
            }
        }
    }

    private int GetNextTrackIndex()
    {
        if (levelSoundtracks.Count <= 1) return 0;
        return (currentTrackIndex + 1) % playlistOrder.Count;
    }

    private int GetPreviousTrackIndex()
    {
        if (levelSoundtracks.Count <= 1) return 0;
        return (currentTrackIndex - 1 + playlistOrder.Count) % playlistOrder.Count;
    }

    // Example method to handle game state changes from other managers
    public void HandleGameStateChanged(string gameState)
    {
        switch (gameState)
        {
            case "MainMenu":
                PlayMainMenuSoundtrack();
                break;
            case "LevelStart":
                PlayLevelSoundtracks();
                break;
            case "LevelComplete":
                PlayLevelCompleteSoundtrack();
                break;
            case "GamePaused":
                PauseCurrentSoundtrack();
                break;
            case "GameResumed":
                ResumeCurrentSoundtrack();
                break;
            default:
                break;
        }
    }
}