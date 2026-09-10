using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Dribble")]
    [SerializeField] private float controlDistance = 1.2f;
    [SerializeField] private float dribbleForce = 30f;
    [SerializeField] private float dribbleSpeed = 5f;
    [SerializeField] private float lobHeight = 0.6f;
    [SerializeField] private float stealCooldown = 0.5f;

    private float stealTimer;
    private Rigidbody rb;
    public bool CanSteal
    {
        get { return stealTimer <= 0f; }
    }
    public MonoBehaviour Owner { get; private set; }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(MonoBehaviour owner)
    {
        Owner = owner;
        stealTimer = stealCooldown;
    }

    public void ClearOwner()
    {
        Owner = null;
    }
    private void Update()
    {
        if(stealTimer>0)
        {
            stealTimer-= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (Owner == null)
            return;

        Vector3 target =
            Owner.transform.position +
            Owner.transform.forward * 0.8f
            +Vector3.up*0.2f;

        Vector3 dir = target - transform.position;

        if (dir.magnitude > controlDistance)
        {
            rb.linearVelocity = new Vector3(
                dir.normalized.x * dribbleSpeed,
                rb.linearVelocity.y,
                dir.normalized.z * dribbleSpeed);
        }
    }
    public void Kick(Vector3 direction,float power,bool isLob)
    {
        ClearOwner();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        if(isLob)
        {
            //少し上方向を加える
            Vector3 lobDir = direction.normalized + Vector3.up * lobHeight;
            rb.AddForce(lobDir.normalized*power,ForceMode.Impulse);
        }
        else
        {
            //グラウンダー
            direction.y = 0;
            rb.AddForce(direction.normalized * power, ForceMode.Impulse);

        }
    }
}