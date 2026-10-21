using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-80)]
[DisallowMultipleComponent]
public class MobileUIRoot : MonoBehaviour
{
    public Canvas referenceCanvas;
    [Tooltip("Assign a bundled Korean font for deployment; otherwise use an available system font.")]
    public Font font;
    [Min(8f)] public float referenceDragThreshold = 24f;
    private bool ownsFont;
    private int lastThreshold;
    private EventSystem lastEventSystem;

    void Awake()
    {
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Noto Sans KR", "Droid Sans Fallback", "Arial" }, 32);
            ownsFont = font != null;
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        ApplyFallbackFont(transform);
    }

    public void ApplyFallbackFont(Transform subtree)
    {
        if (font == null || subtree == null) return;
        foreach (Text label in subtree.GetComponentsInChildren<Text>(true))
            if (label.font == null || label.font.name == "Arial" || label.font.name == "LegacyRuntime") label.font = font;
    }

    void Update()
    {
        EventSystem current = EventSystem.current;
        if (current == null) return;
        float scale = referenceCanvas != null ? referenceCanvas.scaleFactor : Screen.width / 1080f;
        int threshold = Mathf.Max(8, Mathf.RoundToInt(referenceDragThreshold * scale));
        if (current == lastEventSystem && threshold == lastThreshold) return;
        lastEventSystem = current;
        lastThreshold = threshold;
        current.pixelDragThreshold = threshold;
    }

    void OnDestroy() { if (ownsFont && font != null) Destroy(font); }
}
