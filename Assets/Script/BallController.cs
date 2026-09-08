using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Dribble")]
    [SerializeField] private float controlDistance = 1.2f;
    [SerializeField] private float dribbleForce = 30f;
    [SerializeField] private float dribbleSpeed = 5f;
    private Rigidbody rb;

    public PlayerController Owner { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(PlayerController player)
    {
        Owner = player;
    }

    public void ClearOwner()
    {
        Owner = null;
    }

    private void FixedUpdate()
    {
        if (Owner == null)
            return;

        Vector3 target =
            Owner.transform.position +
            Owner.transform.forward * 0.5f;

        Vector3 dir = target - transform.position;

        if (dir.magnitude > controlDistance)
        {
            rb.linearVelocity = new Vector3(
                dir.normalized.x * dribbleSpeed,
                rb.linearVelocity.y,
                dir.normalized.z * dribbleSpeed);
        }
    }
    public void Kick(Vector3 direction,float power)
    {
        ClearOwner();

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction.normalized*power,ForceMode.Impulse);
    }
}