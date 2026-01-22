using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioClip[] tracks;

    private int currentTrackIndex = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // Prevent duplicates
        if (Object.FindObjectsByType<MusicManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        PlayTrack(currentTrackIndex);
    }

    private void Update()
    {
        if (!musicSource.isPlaying && tracks.Length > 0)
        {
            currentTrackIndex = (currentTrackIndex + 1) % tracks.Length;
            PlayTrack(currentTrackIndex);
        }
    }

    private void PlayTrack(int index)
    {
        musicSource.clip = tracks[index];
        musicSource.Play();
    }
}
