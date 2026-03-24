using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public AudioSource bgmAudioSource; 
    public AudioSource sfxAudioSource; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = true; 
        bgmAudioSource.volume = 0.5f;
        bgmAudioSource.Play();
    }

    public void StopBGM()
    {
        bgmAudioSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxAudioSource.PlayOneShot(clip); 
    }
}