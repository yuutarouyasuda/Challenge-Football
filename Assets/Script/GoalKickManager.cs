using UnityEngine;
using UnityEngine.AI;

public class GoalKickManager : MonoBehaviour
{
    public static GoalKickManager Instance;

    [SerializeField] private BallController ball;

    [Header("Goal Kick Position")]
    [SerializeField] private Transform homeGoalKickPoint;
    [SerializeField] private Transform awayGoalKickPoint;

    [Header("Goal Keeper")]
    [SerializeField] private PlayerController homeGoalKeeper;
    [SerializeField] private GoalkeeperAI awayGoalKeeper;
    [SerializeField] private Transform goalKeeperPosition;
    [Header("Home Goal Kick Position")]
    [SerializeField] private Transform dfGoalKickPosition;
    [SerializeField] private PlayerController defender;
    [SerializeField] private Transform mfGoalKickPosition;
    [SerializeField] private PlayerController midfielder;
    [SerializeField] private Transform fwGoalKickPosition;
    [SerializeField] private PlayerController forward;
    [Header("Away Goal Kick Position")]
    [SerializeField] private Transform awayDfGoalKickPosition;
    [SerializeField] private EnemyAI awayDefender;

    [SerializeField] private Transform awayMfGoalKickPosition;
    [SerializeField] private EnemyAI awayMidfielder;

    [SerializeField] private Transform awayFwGoalKickPosition;
    [SerializeField] private EnemyAI awayForward;
    public bool IsGoalKick { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    private void SetHomeGoalKickPosition()
    {
        homeGoalKeeper.transform.position =
            homeGoalKickPoint.position;

        defender.transform.position =
            dfGoalKickPosition.position;

        midfielder.transform.position =
            mfGoalKickPosition.position;

        forward.transform.position =
            fwGoalKickPosition.position;
    }


    private void SetAwayGoalKickPosition()
    {
        awayGoalKeeper.GetComponent<NavMeshAgent>()
            .Warp(awayGoalKickPoint.position);

        awayDefender.GetComponent<NavMeshAgent>()
            .Warp(awayDfGoalKickPosition.position);

        awayMidfielder.GetComponent<NavMeshAgent>()
            .Warp(awayMfGoalKickPosition.position);

        awayForward.GetComponent<NavMeshAgent>()
            .Warp(awayFwGoalKickPosition.position);
    }
    public void StartGoalKick(bool isHomeSide)
    {
        Debug.Log("ゴールキック開始");

        IsGoalKick = true;
        GameManager.Instance.StartGoalKickState();


        // 両チーム配置
        SetHomeGoalKickPosition();
        SetAwayGoalKickPosition();


        // ボールを持つGKだけ決める
        if (isHomeSide)
        {
            ball.ResetBall(homeGoalKickPoint.position);
            ball.SetOwner(homeGoalKeeper);
        }
        else
        {
            ball.ResetBall(awayGoalKickPoint.position);
            ball.SetOwner(awayGoalKeeper);

            awayGoalKeeper.StartGoalKickPass();
        }
    }
    public void EndGoalKick()
    {
        IsGoalKick = false;

        GameManager.Instance.EndGoalKickState();

        Debug.Log("ゴールキック終了");
    }
}