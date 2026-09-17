using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private BallController ball;

    [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -12f);

    [SerializeField] private float followSpeed = 5f;

    private void LateUpdate()
    {
        if (ball == null)
            return;

        Vector3 targetPos = ball.transform.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            followSpeed * Time.deltaTime);

        transform.LookAt(ball.transform.position);
    }
}