using UnityEngine;
using System.Collections;

public class ButtonRotator : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    private RectTransform rectTransform;
    private bool isSpinning = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Spin()
    {
        if (isSpinning) return;
        StartCoroutine(SpinRoutine());
    }

    IEnumerator SpinRoutine()
{
    isSpinning = true;
    
    float elapsed = 0f;
    Vector3 startRotation = rectTransform.localEulerAngles;
    float startZ = startRotation.z;
    float targetZ = startZ + 360f;
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;
        t = t * t * (3f - 2f * t);

        float currentZ = Mathf.Lerp(startZ, targetZ, t);
        rectTransform.localEulerAngles = new Vector3(startRotation.x, startRotation.y, currentZ);
        
        yield return null;
    }

    rectTransform.localEulerAngles = startRotation;
    isSpinning = false;
}
}