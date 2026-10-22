using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class MobileSafeArea : MonoBehaviour
{
    private RectTransform rect;
    private Rect previousArea;
    private Vector2Int previousSize;

    void Awake() { rect = (RectTransform)transform; }
    void OnEnable() => Refresh();
    void Update() => Refresh();

    public void Refresh()
    {
        if (rect == null) rect = (RectTransform)transform;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        Rect area = Screen.safeArea;
        if (size.x <= 0 || size.y <= 0 || (size == previousSize && area == previousArea)) return;
        previousSize = size;
        previousArea = area;
        Rect normalized = Normalize(area, size);
        rect.anchorMin = normalized.min;
        rect.anchorMax = normalized.max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public static Rect Normalize(Rect area, Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f) return new Rect(0f, 0f, 1f, 1f);
        float xMin = Mathf.Clamp01(area.xMin / screenSize.x);
        float yMin = Mathf.Clamp01(area.yMin / screenSize.y);
        float xMax = Mathf.Max(xMin, Mathf.Clamp01(area.xMax / screenSize.x));
        float yMax = Mathf.Max(yMin, Mathf.Clamp01(area.yMax / screenSize.y));
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}
