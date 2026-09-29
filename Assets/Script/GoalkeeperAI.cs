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

    private NavMeshAgent agent;
    private bool hasBall;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (ball == null)
            return;

        if (hasBall)
            return;

        MoveGoalkeeper();
        TryCatchBall();
    }

    //------------------------------------------------
    // GK移動
    //------------------------------------------------
    private void MoveGoalkeeper()
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();

        Vector3 predict = ball.transform.position;

        // ボールの移動先を予測
        if (rb != null)
        {
            predict += rb.linearVelocity * 0.3f;
        }

        Vector3 target = ownGoal.position;

        target.x = Mathf.Clamp(
            predict.x,
            ownGoal.position.x - moveRange,
            ownGoal.position.x + moveRange);

        target.z = ownGoal.position.z;

        // ボールが近いなら少し前へ出る
        float distance =
            Vector3.Distance(ball.transform.position,
                             ownGoal.position);

        if (distance < rushDistance)
        {
            Vector3 dir =
                (ball.transform.position - ownGoal.position).normalized;

            target += dir * 2f;
        }

        agent.SetDestination(target);

        // ボールを見る
        Vector3 look =
            ball.transform.position - transform.position;

        look.y = 0;

        if (look != Vector3.zero)
        {
            transform.forward = look.normalized;
        }
    }

    //------------------------------------------------
    // キャッチ
    //------------------------------------------------
    private void TryCatchBall()
    {
        if (ball.Owner != null)
            return;

        float distance =
            Vector3.Distance(transform.position,
                             ball.transform.position);

        if (distance > catchDistance)
            return;

        ball.SetOwner(this);

        hasBall = true;

        StartCoroutine(PassCoroutine());
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
            Vector3 dir =
                target.position - transform.position;

            dir.y = 0;

            transform.forward = dir.normalized;

            ball.Kick(dir.normalized,
                      passPower,
                      false);
        }

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