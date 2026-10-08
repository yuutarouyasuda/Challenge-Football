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
    public MonoBehaviour LastKicker {  get; private set; }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(MonoBehaviour owner)
    {
        Owner = owner;

        LastKicker = null;
        stealTimer = stealCooldown;
        IsPassing = false;

        Vector3 target =
            owner.transform.position +
            owner.transform.forward * 0.8f +
            Vector3.up * 0.2f;

        rb.position = target;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
         
        if(owner is TeammateAI mate)
        {
            mate.isReceiver = false;
        }

        PlayerController player =owner.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetCurrentBall(this);
        }
       // GameManager.Instance.OnBallOwnerChanged(owner);
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

        // 所有者を取得
        PlayerController player = Owner as PlayerController;
        EnemyAI enemy = Owner as EnemyAI;

        // 初期値
        float currentHoldDistance = holdDistance;
        float currentDribbleSpeed = dribbleSpeed;

        // プレイヤーならPlayerDataのドリブル能力を使用
        if (player != null)
        {
            currentHoldDistance = Mathf.Lerp(1.5f, 0.8f, player.Dribble / 100f);
            currentDribbleSpeed = Mathf.Lerp(4f, 12f, player.Dribble / 100f);
        }
        // 敵ならEnemyAIのドリブル能力を使用
        else if (enemy != null)
        {
            currentHoldDistance = Mathf.Lerp(1.5f, 0.8f, enemy.Dribble / 100f);
            currentDribbleSpeed = Mathf.Lerp(4f, 12f, enemy.Dribble / 100f);
        }

        // ボールの目標位置
        Vector3 target =
            Owner.transform.position +
            Owner.transform.forward * currentHoldDistance +
            Vector3.up * holdHeight;

        // 一定速度で目標位置へ近づける
        Vector3 nextPosition = Vector3.MoveTowards(
            rb.position,
            target,
            currentDribbleSpeed * Time.fixedDeltaTime);

        rb.MovePosition(nextPosition);

        // ボールが勝手に転がらないようにする
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

    }
    public void ResetBall(Vector3 position)
    {
        transform.position = position;

        Rigidbody rb=GetComponent<Rigidbody>();

        if(rb!=null)
        {
            rb.linearVelocity=Vector3.zero;
            rb.angularVelocity=Vector3.zero;
        }

        Owner = null;
    }
    public void Kick(Vector3 direction,float power,bool isLob)
    {
        LastKicker = Owner;

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
    }
}