using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    public float stopThreshold = 0.1f;
    public float returnSpeed = 4f;

    private Vector3 targetPosition;
    private Vector3 currentVelocity = Vector3.zero;

    void FixedUpdate()
    {
        Vector3 playerVelocity = (player.position - targetPosition) / Time.deltaTime;
        bool isPlayerMoving = playerVelocity.magnitude > stopThreshold;

        if (isPlayerMoving)
        {
            targetPosition = player.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothSpeed);
        }
        else
        {
            Vector3 centerPosition = player.position + offset;
            transform.position = Vector3.Lerp(transform.position, centerPosition, Time.deltaTime * returnSpeed);
        }
    }
}