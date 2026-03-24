using UnityEngine;

public class RandomBGMPlayer : MonoBehaviour
{
    public AudioClip[] sceneBGMList;

    void Start()
    {
        if (SoundManager.Instance != null && sceneBGMList != null && sceneBGMList.Length > 0)
        {
            int randomIndex = Random.Range(0, sceneBGMList.Length);
            AudioClip selectedBGM = sceneBGMList[randomIndex];

            SoundManager.Instance.PlayBGM(selectedBGM);
            
            Debug.Log($"노래: {selectedBGM.name}");
        }
        else if (sceneBGMList == null || sceneBGMList.Length == 0)
        {
            Debug.LogWarning("노래 리스트 X");
        }
    }
}