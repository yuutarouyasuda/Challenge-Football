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
    [SerializeField] private float autoSwitchRange = 4f;
    [SerializeField] private float autSwitchInterval = 0.2f;
    private float autoSwitchTimer;
    public MonoBehaviour PassTarget { get; private set; }
    private float stealTimer;
    private Rigidbody rb;
    public bool CanSteal
    {
        get { return stealTimer <= 0f; }
    }
    public bool IsPassing { get; private set; }
    public MonoBehaviour Owner { get; private set; }
    public MonoBehaviour LastKicker {  get; private set; }

    public MonoBehaviour LastOwner { get; private set; }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(MonoBehaviour owner)
    {

        PassTarget = null;

        bool wasHome = Owner is PlayerController || Owner is TeammateAI_New;

        bool nowHome=owner is PlayerController || owner is TeammateAI_New;

        if (Owner!=null&&wasHome!=nowHome)
        {
            ChallengeManager.Instance.ResetProgress(wasHome);
        }
        Owner = owner;
        if (IsPassing)
        {
            if (owner is PlayerController || owner is TeammateAI_New)
            {
                ChallengeManager.Instance.OnPassReceive(true);
            }
            else if (owner is EnemyAI)
            {
                ChallengeManager.Instance.OnPassReceive(false);
            }
        }
        stealTimer = stealCooldown;
        IsPassing = false;

        Vector3 target;

        if (owner.GetComponent<GoalkeeperAI>() != null)
        {
            target = owner.transform.position;
        }
        else
        {
            target =
                owner.transform.position +
                owner.transform.forward * 2f +
                Vector3.up * 0.2f;
        }

        rb.position = target;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
         
        if(owner is TeammateAI_New mate)
        {
            mate.isReceiver = false;
        }

        PlayerController player =owner.GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetCurrentBall(this);
        }

        EnemyAI enemy =Owner.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.SetCurrentBall(this);
        }
       // GameManager.Instance.OnBallOwnerChanged(owner);
    }

    public void ClearOwner()
    {
        Debug.Log("ClearOwner");
        Owner = null;
    }
    private void Update()
    {
        if (stealTimer > 0)
        {
            stealTimer -= Time.deltaTime;
        }
        if (Owner == null)
        {
            autoSwitchTimer-=Time.deltaTime;

            if(autoSwitchTimer<=0)
            {
                autoSwitchTimer = autSwitchInterval;
                AutoSwitchPlayer();
            }
        }
        // パス対象への自動切り替え
        if (PassTarget != null && Owner == null)
        {
            float distance = Vector3.Distance(
                transform.position,
                PassTarget.transform.position);

            if (distance < 10f)
            {
                PlayerController player =
                    PassTarget.GetComponent<PlayerController>();

                if (player != null)
                {
                    GameManager.Instance.ChangePlayer(player);
                }

                PassTarget = null;
            }
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
            currentDribbleSpeed =
                Mathf.Lerp(
                    3f,
                    8f,
                    player.Dribble / 100f);
        }
        // 敵ならEnemyAIのドリブル能力を使用
        else if (enemy != null)
        {
            currentHoldDistance = Mathf.Lerp(1.5f, 0.8f, enemy.Dribble / 100f);
            currentDribbleSpeed =
                Mathf.Lerp(
                    3f,
                    8f,
                    enemy.Dribble / 100f);
        }
        Rigidbody ownerRb = Owner.GetComponent<Rigidbody>();

        if (ownerRb != null)
        {
            currentHoldDistance += ownerRb.linearVelocity.magnitude * 0.05f;
        }
        // ボールの目標位置
        Vector3 moveDir;

        if (ownerRb != null && ownerRb.linearVelocity.magnitude > 0.1f)
        {
            moveDir = ownerRb.linearVelocity.normalized;
        }
        else
        {
            moveDir = Owner.transform.forward;
        }

        Vector3 target =
            Owner.transform.position +
            moveDir * currentHoldDistance +
            Vector3.up * holdHeight;

        // 一定速度で目標位置へ近づける
        rb.MovePosition(
      Vector3.MoveTowards(
          rb.position,
          target,
          currentDribbleSpeed * 3f * Time.fixedDeltaTime));

        // ドリブル中のボール速度
        float currentDribbleForce = dribbleForce;

        if (player != null)
        {
            currentDribbleForce =
                Mathf.Lerp(
                    15f,
                    40f,
                    player.Dribble / 100f);
        }

        // 少しだけ前へ押し出す
        //rb.AddForce(
        //  Owner.transform.forward * currentDribbleForce,
        //ForceMode.Acceleration);
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
    public void Kick(Vector3 direction,float power,bool isLob,MonoBehaviour target = null)
    {
        if (GoalKickManager.Instance != null && GoalKickManager.Instance.IsGoalKick)
        {
            GoalKickManager.Instance.EndGoalKick();
        }
        LastKicker = Owner;

        PassTarget = target;
        Debug.Log("Kick");
        ClearOwner();
        Debug.Log("kick後 Owner=" + Owner);
        stealTimer = stealCooldown;
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
    private void OnCollisionEnter(Collision collision)
    {
        if (Owner != null)
            return;

        if (stealTimer > 0f)
            return;

        EnemyAI enemy = collision.collider.GetComponentInParent<EnemyAI>();

        if (enemy != null && enemy != LastKicker)
        {
            SetOwner(enemy);
            return;
        }

        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();

        if (player != null && player != LastKicker)
        {
            SetOwner(player);
        }
    }
    private void AutoSwitchPlayer()
    {
        Debug.Log("AutoSwitch");

        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        PlayerController nearest = null;
        float best = float.MaxValue;
        foreach (PlayerController player in players)
        {
            if (player.GetComponent<TeammateAI_New>() == null)
                continue;

            float distance =
                Vector3.Distance(transform.position,
                                 player.transform.position);


            if (distance < best)
            {
                best = distance;
                nearest = player;
            }
        }


        if (nearest != null)
        {

            if (GameManager.Instance.CurrentPlayer != nearest)
            {
                Debug.Log("切り替え実行");
                GameManager.Instance.ChangePlayer(nearest);

                PlayerSwitchManager switchManager =
    FindAnyObjectByType<PlayerSwitchManager>();

                if (switchManager != null)
                {
                    switchManager.SetCurrentPlayer(nearest);
                }
            }
        }
        else
        {
            Debug.Log("Nearestなし");
        }

    }

}