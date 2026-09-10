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
    private BallController currentBall;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
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
                        if (ballController.CanSteal && ballController.Owner != this)
                        {
                            ballController.SetOwner(this);
                            currentBall = ballController;
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
            agent.SetDestination(opponentGoal.position);

            float distance = Vector3.Distance(
                transform.position,
                opponentGoal.position);
            if (distance < shootDistance)
            {
                //ゴール方向を計算
                Vector3 shootDir = opponentGoal.position - transform.position;
                shootDir.y = 0f;
                //ゴールの方向を向く
                transform.forward = shootDir.normalized;
                //シュート
                currentBall.Kick(shootDir.normalized, shootPower, false);
                currentBall = null;
            }
        }
    }

}
