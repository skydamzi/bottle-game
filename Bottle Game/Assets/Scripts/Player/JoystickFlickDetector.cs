using System;
using UnityEngine;

public class JoystickFlickDetector : MonoBehaviour
{
    public FixedJoystick joystick;
    public bool flickEnabled = true;
    [Range(0f, 0.5f)] public float startThreshold = 0.2f;
    [Range(0.5f, 1f)] public float reachThreshold = 0.85f;
    [Min(0.01f)] public float maxFlickTime = 0.12f;
    [Min(0f)] public float minimumDragPixels = 12f;
    public bool logFlicks;
    [SerializeField] private string debugStatus = "Waiting for touch";
    public string DebugStatus => debugStatus;
    public event Action<Vector2> FlickDetected;

    private FixedJoystick boundJoystick;
    private bool candidate;
    private float startedAt;
    private Vector2 startInput;

    void OnEnable() => Bind();
    void Update() { if (boundJoystick != joystick) { Unbind(); Bind(); } }
    void OnDisable() => Unbind();

    private void Bind()
    {
        if (joystick == null) return;
        boundJoystick = joystick;
        boundJoystick.TouchStarted += OnTouchStarted;
        boundJoystick.TouchMoved += OnTouchMoved;
        boundJoystick.TouchEnded += CancelGesture;
        boundJoystick.TouchCancelled += CancelGesture;
    }

    private void Unbind()
    {
        if (boundJoystick != null)
        {
            boundJoystick.TouchStarted -= OnTouchStarted;
            boundJoystick.TouchMoved -= OnTouchMoved;
            boundJoystick.TouchEnded -= CancelGesture;
            boundJoystick.TouchCancelled -= CancelGesture;
        }
        boundJoystick = null;
        CancelGesture();
    }

    private void OnTouchStarted(Vector2 input, float time)
    {
        startedAt = time;
        startInput = input;
        candidate = flickEnabled && input.magnitude <= startThreshold;
        debugStatus = candidate ? "Armed: center touch" : "Movement: touch outside center";
    }

    private void OnTouchMoved(Vector2 input, float time)
    {
        if (!candidate || !flickEnabled || boundJoystick == null || !boundJoystick.IsPointerDown) return;
        float elapsed = time - startedAt;
        if (elapsed > maxFlickTime) { candidate = false; debugStatus = "Movement: flick timeout"; return; }
        if (elapsed <= 0f || input.magnitude < Mathf.Max(startThreshold + 0.01f, reachThreshold)) return;
        if (Vector2.Distance(boundJoystick.PointerStartPosition, boundJoystick.PointerPosition) < minimumDragPixels) return;
        if ((input - startInput).sqrMagnitude < 0.01f) return;
        candidate = false; // One gesture per real touch; direction changes never re-arm it.
        debugStatus = $"Flick {input.normalized} in {elapsed:F3}s";
        if (logFlicks) Debug.Log(debugStatus, this);
        FlickDetected?.Invoke(input.normalized);
    }

    public void CancelGesture() { candidate = false; debugStatus = "Waiting for new touch"; }
}
