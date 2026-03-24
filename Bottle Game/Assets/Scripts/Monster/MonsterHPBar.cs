using UnityEngine;

public class MonsterHealthBar : MonoBehaviour
{
    public GameObject hpFillSprite;
    private MonsterStats stats;
    private Quaternion fixedRotation;
    
    void Awake()
    {
        stats = GetComponentInParent<MonsterStats>();
        fixedRotation = transform.rotation;
    }

    void OnEnable()
    {
        if (stats != null) stats.OnHealthChanged += UpdateHPBar;
    }

    void OnDisable()
    {
        if (stats != null) stats.OnHealthChanged -= UpdateHPBar;
    }

    void LateUpdate()
    {
        transform.rotation = fixedRotation;
    }

    private void UpdateHPBar(float current, float max)
    {
        float ratio = Mathf.Clamp01(current / max);
        hpFillSprite.transform.localScale = new Vector3(ratio, 1, 1);
    }
}