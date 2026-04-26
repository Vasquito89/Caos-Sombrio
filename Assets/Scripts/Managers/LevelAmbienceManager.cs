using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class LevelAmbienceManager : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("Playlist del Nivel")]
    [SerializeField] private List<AudioClip> levelTracks = new List<AudioClip>();

    [Header("Ajustes")]
    [SerializeField] private bool shuffle = true;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private int currentIndex = 0;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.loop = false; // Queremos que pase a la siguiente

        if (levelTracks.Count > 0)
        {
            if (shuffle) ShuffleTracks();
            PlayCurrentTrack();
        }
    }

    private void Update()
    {
        // Si termina una canción, pasa a la siguiente
        if (!audioSource.isPlaying && levelTracks.Count > 0)
        {
            NextTrack();
        }
    }

    private void PlayCurrentTrack()
    {
        audioSource.clip = levelTracks[currentIndex];
        audioSource.Play();
    }

    private void NextTrack()
    {
        currentIndex = (currentIndex + 1) % levelTracks.Count;
        PlayCurrentTrack();
    }

    private void ShuffleTracks()
    {
        for (int i = 0; i < levelTracks.Count; i++)
        {
            AudioClip temp = levelTracks[i];
            int randomIndex = Random.Range(i, levelTracks.Count);
            levelTracks[i] = levelTracks[randomIndex];
            levelTracks[randomIndex] = temp;
        }
    }
}