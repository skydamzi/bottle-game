using UnityEngine;

public interface IAimProvider
{
    Quaternion FinalAimRotation { get; }
    bool IsInputActive { get; }
    void ResetAim();
}