using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GoalkeeperAI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform ownGoal;
    [SerializeField] private BallController ball;
    [SerializeField] private Transform[] passTargets;

    [Header("Move")]
    [SerializeField] private float moveRange = 4f;
    [SerializeField] private float catchDistance = 2f;
    [SerializeField] private float rushDistance = 10f;

    [Header("Pass")]
    [SerializeField] private float passPower = 15f;
    [SerializeField] private float pickupCooldown = 0.5f;
    [Header("Positioning")]
    [SerializeField] private float returnDistance = 30f;
    [SerializeField] private float farDistance = 25f;
    [SerializeField] private float middleDistance = 15f;
    [SerializeField] private float nearDistance = 8f;

    [SerializeField] private float farForward = 2f;
    [SerializeField] private float middleForward = 4f;
    [SerializeField] private float nearForward = 6f;
    [SerializeField] private float veryNearForward = 8f;
    [Header("Dive")]
    [SerializeField] private float diveSpeed = 12f;
    [SerializeField] private float diveDistance = 3f;
    [SerializeField] private float diveBallSpeed = 10f;
    [SerializeField] private float diveCooldown = 1f;
    [SerializeField] private float goalLineOffset = 0.5f;
    [SerializeField] private float diveReactionDistance = 15f;
    [SerializeField] private float shotSpeed = 12f;   // シュートと判断する速度
    [SerializeField] private float goalWidth = 3.5f;  // ゴール幅
    [Header("Punch")]
    [SerializeField] private float punchShotSpeed = 18f;
    [SerializeField] private float punchPower = 20f;
    [SerializeField] private float saveDecisionDistance = 8f;
    private bool isDiving;
    private float diveTimer;
    [SerializeField] private bool isHomeGoalkeeper;
    private float pickupTimer;
    private NavMeshAgent agent;
    private bool hasBall;
    private PlayerController player;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (pickupTimer > 0f)
        {
            pickupTimer -= Time.deltaTime;
        }

        if (ball == null)
            return;

        if (hasBall)
            return;

        if (isDiving)
        {
            diveTimer -= Time.deltaTime;

            if (diveTimer <= 0f)
                isDiving = false;

            return;   // ダイビング中は通常移動しない
        }

        MoveGoalkeeper();
        TryDiveSave();
        TryCatchBall();
    }

    //------------------------------------------------
    // GK移動
    //------------------------------------------------
    private void MoveGoalkeeper()
    {
        if (!agent.enabled)
            return;
        if(!agent.isOnNavMesh) 
            return;
        Rigidbody rb = ball.GetComponent<Rigidbody>();

        Vector3 predict = ball.transform.position;

        if (rb != null)
        {
            predict += rb.linearVelocity * 0.3f;
        }

        // ゴール→ボール方向
        Vector3 dir = (predict - ownGoal.position).normalized;

        // ゴールからボールまでの距離
        float goalToBall =
            Vector3.Distance(ownGoal.position, predict);

        Vector3 target;

        // ボールが遠いならゴール中央へ戻る
        if (goalToBall > returnDistance)
        {
            target = ownGoal.position;
        }
        else
        {
            float forwardDistance;
            if (goalToBall > farDistance)
            {
                forwardDistance = farForward;
            }
            else if (goalToBall > middleDistance)
            {
                forwardDistance = middleForward;
            }
            else if (goalToBall > nearDistance)
            {
                forwardDistance = nearForward;
            }
            else
            {
                forwardDistance = veryNearForward;
            }

            Vector3 goalBase = ownGoal.position;

            // ボールが左右に寄ったらGKも少し寄る
            goalBase.z = Mathf.Lerp(
                ownGoal.position.z,
                predict.z,
                0.6f);

            target = goalBase + dir * forwardDistance;
        }

        // ペナルティエリア内に制限
        if (isHomeGoalkeeper)
        {
            // 味方GK（Xが小さくなる方向へ前に出る）
            target.x = Mathf.Clamp(
                target.x,
                ownGoal.position.x - rushDistance,
                ownGoal.position.x);
        }
        else
        {
            // 敵GK（Xが大きくなる方向へ前に出る）
            target.x = Mathf.Clamp(
                target.x,
                ownGoal.position.x,
                ownGoal.position.x + rushDistance);
        }
        target.z = Mathf.Clamp(
     target.z,
     ownGoal.position.z - moveRange,
     ownGoal.position.z + moveRange);
        // Debug.Log("Target = " + target);
        agent.SetDestination(target);

        // ボールを見る
        Vector3 look = predict - transform.position;
        look.y = 0;

        if (look != Vector3.zero)
        {
            transform.forward = look.normalized;
        }
    }
    private void TryDiveSave()
    {
        if (isDiving)
        {
            return;
        }

        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (rb == null)
        {
            return;
        }
        float distance = Vector3.Distance(
    transform.position,
    ball.transform.position);

        // ボールが十分近づくまで待つ
        if (distance > saveDecisionDistance)
            return;
        Vector3 velocity = rb.linearVelocity;


        if (velocity.magnitude < shotSpeed)
        {
            return;
        }

        float goalX = ownGoal.position.x;

        if (Mathf.Abs(velocity.x) < 0.01f)
        {
            return;
        }

        float t =
            (goalX - ball.transform.position.x) /
            velocity.x;


        if (t < 0)
        {
            return;
        }

        float predictZ =
            ball.transform.position.z +
            velocity.z * t;


        float leftPost = ownGoal.position.z - goalWidth * 0.5f;
        float rightPost = ownGoal.position.z + goalWidth * 0.5f;

        if (predictZ < leftPost || predictZ > rightPost)
        {
            return;
        }

        float diff = predictZ - transform.position.z;


        if (Mathf.Abs(diff) < 0.5f)
        {
            return;
        }


        Vector3 diveDir =
            diff > 0 ? transform.right : -transform.right;

        StartCoroutine(DiveCoroutine(diveDir));
    }
    private IEnumerator DiveCoroutine(Vector3 dir)
    {
        isDiving = true;

        agent.enabled = false;

        Rigidbody body = GetComponent<Rigidbody>();

        body.linearVelocity =
            dir.normalized * diveSpeed;

        yield return new WaitForSeconds(0.35f);

        body.linearVelocity = Vector3.zero;

        agent.enabled = true;

        diveTimer = diveCooldown;
    }
    //------------------------------------------------
    // キャッチ
    //------------------------------------------------
    private void TryCatchBall()
    {
        if (pickupTimer > 0)
            return;
        if (ball.Owner != null)
            return;
        Rigidbody rb = ball.GetComponent<Rigidbody>();

        float distance =
      Vector3.Distance(transform.position,
                       ball.transform.position);

        if (rb != null &&
            rb.linearVelocity.magnitude >= punchShotSpeed &&
            distance <= catchDistance + diveDistance)
        {
            PunchBall();
            return;
        }

        float saveDistance = catchDistance;
        if (isDiving)
        {
            saveDistance += diveDistance;
        }
        if (distance > saveDistance)
            return;

        if (player != null)
        {
            // 味方GK
            ball.SetOwner(player);

            hasBall = true;

            GameManager.Instance.ChangePlayer(player);
        }
        else
        {
            // 敵GK
            ball.SetOwner(this);

            hasBall = true;

            StartCoroutine(PassCoroutine());
        }

    }
    private void PunchBall()
    {
        ball.ClearOwner();

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        Vector3 dir;

        if (ball.transform.position.z > transform.position.z)
        {
            // ボールがGKの右側
            dir = (transform.forward + transform.right).normalized;
        }
        else
        {
            // ボールがGKの左側
            dir = (transform.forward - transform.right).normalized;
        }

        dir.y = 0.2f;

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(dir * punchPower, ForceMode.Impulse);

        dir.y = 0.2f;

        pickupTimer = pickupCooldown;
    }
    //------------------------------------------------
    // 味方へパス
    //------------------------------------------------
    private IEnumerator PassCoroutine()
    {

        yield return new WaitForSeconds(1f);

        Transform target = FindBestPassTarget();


        if (target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0;

            transform.forward = dir.normalized;


            ball.Kick(dir.normalized, passPower, false);

            hasBall = false;

            pickupTimer = pickupCooldown;
        }
        pickupTimer = pickupCooldown;
        hasBall = false;
    }

    //------------------------------------------------
    // 一番近い味方を探す
    //------------------------------------------------
    private Transform FindBestPassTarget()
    {
        Transform best = null;

        float nearest = Mathf.Infinity;

        foreach (Transform mate in passTargets)
        {
            if (mate == null)
                continue;

            float d =
                Vector3.Distance(transform.position,
                                 mate.position);

            if (d < nearest)
            {
                nearest = d;
                best = mate;
            }
        }

        return best;
    }

}