using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum DefenderRole
    {
        Press,
        CutPass,
        CoverGoal
    }

    public DefenderRole Role { get; set; }
    [SerializeField] private Transform ball;
    [SerializeField] private Transform opponentGoal;
    [SerializeField] private float shootPower = 20f;
    [SerializeField] private float shootDistance = 8f;
    [SerializeField] private Transform ownGoal;
    [SerializeField] private bool canShoot = false;
    [SerializeField] private EnemyAI[] teammates;
    [SerializeField] private float passPower = 12f;
    [SerializeField] private float passCooldown = 1.0f;
    private float passTimer = 0f;
    private BallController currentBall;
    private NavMeshAgent agent;
    private float noPickupTimer = 0f;
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        if(noPickupTimer>0f)
        {
            noPickupTimer -= Time.deltaTime;
        }
        if (ball == null)
            return;

        BallController ballController = ball.GetComponent<BallController>();

        //ボールを失ったら所有権を失う
        if (currentBall != null && ballController.Owner != this)
        {
            currentBall = null;
        }

        // ボールを持っていない
        if (currentBall == null)
        {

            switch (Role)
            {
                case DefenderRole.Press:
                    // ボールへ
                    agent.SetDestination(ball.position);
                    
                    float distance=Vector3.Distance(transform.position,ball.position);
                    if (distance < 1.2f)
                    {
                        if (noPickupTimer <= 0f &&
                            ballController.CanSteal &&
                            ballController.Owner != this)
                        {
                            ballController.SetOwner(this);
                            currentBall = ballController;
                            passTimer = passCooldown;
                        }
                    }
                    break;

                case DefenderRole.CutPass:
                    // パスコースへ
                    if (ballController.Owner != null)
                    {
                        // ボール保持者
                        Vector3 ballPos = ball.position;

                        // 自ゴール方向へ少し下がった位置
                        Vector3 target =
                            Vector3.Lerp(ballPos, opponentGoal.position, 0.4f);

                        agent.SetDestination(target);
                    }

                    break;

                case DefenderRole.CoverGoal:
                    // ゴール前へ
                    agent.SetDestination(ownGoal.position);
                    break;
            }
        }
        // ボールを持っている
        else
        {
            passTimer -= Time.deltaTime;
            //Debug.Log(passTimer);   
            agent.SetDestination(opponentGoal.position);

            float distance = Vector3.Distance(
                transform.position,
                opponentGoal.position);
            if (distance < shootDistance && canShoot)
            {
                Vector3 shootDir=opponentGoal.position-transform.position;

                shootDir.y = 0f;

                transform.forward = shootDir.normalized;
                currentBall.Kick(shootDir.normalized, shootPower, false);
                currentBall = null;
            }
            else
            {
                EnemyAI receiver = FindBestReceiver();

                if (receiver != null&&passTimer<=0f)
                {
                    PassBall();
                    passTimer = passCooldown;
                }
            }
        }
    }
    private EnemyAI FindBestReceiver()
    {
        EnemyAI best = null;
        float bestScore = float.MinValue;

        foreach (EnemyAI mate in teammates)
        {
            if (mate == null)
                continue;

            if (mate == this)
                continue;

            float score = 0;

            // ゴールに近いほど高評価
            score -= Vector3.Distance(
                mate.transform.position,
                opponentGoal.position);

            if (score > bestScore)
            {
                bestScore = score;
                best = mate;
            }
        }
        
        return best;

    }
    private void PassBall()
    {
        EnemyAI receiver = FindBestReceiver(); ;

        if (receiver == null)
            return;

        Vector3 dir =
            receiver.transform.position - transform.position;

        dir.y = 0;

        currentBall.Kick(dir.normalized, passPower, false);

        currentBall = null;

        noPickupTimer = 0.5f;
    }
}
