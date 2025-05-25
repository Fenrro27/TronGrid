using UnityEngine;

public class MusicController : MonoBehaviour
{
    public AudioClip[] musicClips;         // Array con la música
    private AudioSource audioSource;        // Componente AudioSource

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        PlayRandomMusic();
    }

    // Update is called once per frame
    void Update()
    {
        if (!audioSource.isPlaying)
        {
            PlayRandomMusic();
        }
    }
    void PlayRandomMusic()
    {
        if (musicClips.Length == 0) return;

        int randomIndex = Random.Range(0, musicClips.Length); // Selección aleatoria
        audioSource.clip = musicClips[randomIndex];
        audioSource.Play();
    }

}
