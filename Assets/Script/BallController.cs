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
    [Header("Ball Position")]
    [SerializeField] private float holdDistance = 1.2f;
    [SerializeField] private float holdHeight = 0.05f;
    private float stealTimer;
    private Rigidbody rb;
    public bool CanSteal
    {
        get { return stealTimer <= 0f; }
    }
    public bool IsPassing { get; private set; }
    public MonoBehaviour Owner { get; private set; }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(MonoBehaviour owner)
    {
        Owner = owner;
        stealTimer = stealCooldown;
        IsPassing = false;

        Vector3 target =
            owner.transform.position +
            owner.transform.forward * 0.8f +
            Vector3.up * 0.2f;

        rb.position = target;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

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
    Owner.transform.forward * holdDistance +
    Vector3.up * holdHeight;

        // ボールを足元に固定
        rb.MovePosition(target);

        // ボールが勝手に転がらないようにする
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

    }
    public void Kick(Vector3 direction,float power,bool isLob)
    {
        ClearOwner();

        IsPassing = true;
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
        Debug.Log(rb.linearVelocity);
    }
}