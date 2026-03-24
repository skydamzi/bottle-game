using UnityEngine;

public class SceneBGMSetter : MonoBehaviour
{
    public AudioClip sceneBGM;

    void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM(sceneBGM);
        }
    }
}