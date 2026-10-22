using UnityEngine;

// Presentation only: selection and effective range belong to PlayerAutoTarget.
// World-space lines have no colliders and never participate in input or combat.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAutoTarget))]
public class PlayerTargetingView : MonoBehaviour
{
    public bool showRange = true;
    public bool showTargetMarker = true;
    [Min(0.005f)] public float rangeWidth = 0.025f;
    [Min(0.005f)] public float markerWidth = 0.045f;
    [Min(0.05f)] public float markerSize = 0.2f;
    [Min(0f)] public float markerOffset = 0.2f;
    public Color rangeColor = new Color(0.35f, 0.9f, 0.9f, 0.25f);
    public Color targetColor = new Color(1f, 0.85f, 0.2f, 0.95f);

    private PlayerAutoTarget targeting;
    private LineRenderer rangeLine, markerLine;
    private Material lineMaterial;
    private readonly Vector3[] circle = new Vector3[64];
    private readonly Vector3[] unitCircle = new Vector3[64];
    private Transform markedTarget;
    private SpriteRenderer targetSprite;
    private Collider2D targetCollider;

    void Awake()
    {
        targeting = GetComponent<PlayerAutoTarget>();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) { enabled = false; return; }
        lineMaterial = new Material(shader);
        for (int i = 0; i < unitCircle.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / unitCircle.Length;
            unitCircle[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }
        rangeLine = CreateLine("Attack Range Outline", circle.Length, 5);
        markerLine = CreateLine("Target Arrow", 3, 100);
    }

    private LineRenderer CreateLine(string lineName, int count, int order)
    {
        var obj = new GameObject(lineName);
        obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial;
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = count;
        line.numCornerVertices = 2;
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) line.sortingLayerID = sprite.sortingLayerID;
        line.sortingOrder = order;
        line.enabled = false;
        return line;
    }

    void LateUpdate()
    {
        if (rangeLine == null || markerLine == null) return;
        bool active = targeting != null && targeting.IsTrackingActive;
        rangeLine.enabled = active && showRange && targeting.CurrentRange > 0f;
        markerLine.enabled = active && showTargetMarker && targeting.HasTarget;
        if (rangeLine.enabled)
        {
            float radius = targeting.CurrentRange;
            for (int i = 0; i < circle.Length; i++)
            {
                circle[i] = transform.position + unitCircle[i] * radius;
            }
            rangeLine.SetPositions(circle);
            rangeLine.startWidth = rangeLine.endWidth = rangeWidth;
            rangeLine.startColor = rangeLine.endColor = rangeColor;
        }
        if (!markerLine.enabled) { markedTarget = null; return; }
        Transform target = targeting.CurrentTarget;
        if (markedTarget != target)
        {
            markedTarget = target;
            targetSprite = target.GetComponent<SpriteRenderer>();
            targetCollider = target.GetComponentInChildren<Collider2D>();
        }
        float top = targetSprite != null ? targetSprite.bounds.max.y :
            targetCollider != null ? targetCollider.bounds.max.y : target.position.y + 0.5f;
        Vector3 tip = new Vector3(target.position.x, top + markerOffset, target.position.z);
        markerLine.SetPosition(0, tip);
        markerLine.SetPosition(1, tip + new Vector3(-markerSize, markerSize * 1.3f, 0f));
        markerLine.SetPosition(2, tip + new Vector3(markerSize, markerSize * 1.3f, 0f));
        markerLine.startWidth = markerLine.endWidth = markerWidth;
        markerLine.startColor = markerLine.endColor = targetColor;
    }

    void OnDisable()
    {
        if (rangeLine != null) rangeLine.enabled = false;
        if (markerLine != null) markerLine.enabled = false;
        markedTarget = null;
    }

    void OnDestroy()
    {
        if (lineMaterial != null) Destroy(lineMaterial);
        if (rangeLine != null) Destroy(rangeLine.gameObject);
        if (markerLine != null) Destroy(markerLine.gameObject);
    }
}
