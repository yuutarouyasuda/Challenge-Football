using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset=new Vector3(0f,12f,-8f);

    [SerializeField] private float followSpeed = 5f;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;

        transform.position=Vector3.Lerp(
            transform.position, targetPos, followSpeed*Time.deltaTime );

        transform.LookAt( target );
    }
}
