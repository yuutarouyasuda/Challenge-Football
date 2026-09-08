using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Transform ball;
    [SerializeField] private Transform opponentGoal;
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

        // ボールを持っていない
        if (currentBall == null)
        {
            agent.SetDestination(ball.position);

            float distance = Vector3.Distance(transform.position, ball.position);

            if (ballController.Owner == null && distance < 1.2f)
            {
                ballController.SetOwner(this);
                currentBall = ballController;
            }
        }
        // ボールを持っている
        else
        {
            agent.SetDestination(opponentGoal.position);

            float distance = Vector3.Distance(
                transform.position,
                opponentGoal.position);

            if (distance < 8f)
            {
                currentBall.Kick(transform.forward, 20f);
                currentBall = null;
            }
        }
    }

}
