using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting.Antlr3.Runtime;
[RequireComponent(typeof(NavMeshAgent))]
public class TeammateAI : MonoBehaviour
{
    public enum TeammateRole
    {
        CutPass,
        CoverGoal,

        Support,
        RunBehind,
    }

    public TeammateRole Role
    {
        get;
        set;
    }
    public bool isReceiver {  get; set; }
    [SerializeField] private Transform ownGoal;
    [Header("Reference")]
    [SerializeField] private Transform opponentGoal;
    [SerializeField] private EnemyAI[] enemies;
    [SerializeField] private TeammateAI[] teammates;
    [Header("Move")]
    [SerializeField] private float forwardDistance = 6f;
    [SerializeField] private float sideDistance = 4f;
    [SerializeField] private float avoidRadius=2f;
    [SerializeField] private float interceptDistance = 1.5f;
    private NavMeshAgent agent;
    private BallController currentBall;
    private PlayerController playerController;
    private void Awake()
    {
        playerController=GetComponent<PlayerController>();
        agent=GetComponent<NavMeshAgent>();
    }
    
    void Update()
    {
        if (!agent.enabled)
            return;
        if (playerController != null && playerController.IsControlled)
        {
            agent.ResetPath();
            agent.isStopped = true;
            return;
        }
        agent.isStopped = false;
        bool isAttack = false;

        BallController ballController = FindFirstObjectByType<BallController>();

        if (ballController == null)
            return;

        if (ballController!=null&&ballController.IsPassing&&isReceiver)
        {
            //受け手は積極的に追う
            InterceptBall(ballController);
            return;
        }
        //受けて以外でも近ければ取得できる
        float d=Vector3.Distance(transform.position, ballController.transform.position);

        if(d<2f&&ballController.Owner==null)
        {
           TakeBall(ballController);
        }
        if (currentBall!=null&&ballController.Owner!=this)
        {
            currentBall=null;
        }
        if (ballController != null)
        {
            //プレイヤーまたは味方が持っていたら攻撃
            if(ballController.Owner is PlayerController||ballController.Owner is TeammateAI)
            {
                isAttack = true;
            }
        }
        if (isAttack)
        {
            AttackMove();
        }
        else
        {
            DefenceMove();
        }
    }
    private float EvaluatePositiion(Vector3 point)
    {
        float score = 0;

        //ゴールに近いほど高評価
        PlayerController current = GameManager.Instance.CurrentPlayer;

        score -= Vector3.Distance(point, opponentGoal.position);

        Vector3 goalDir =
        (opponentGoal.position - current.transform.position).normalized;

        float forward =
            Vector3.Dot(point - current.transform.position, goalDir);

        score += forward * 5f;

        float playerDistanceToPoint=Vector3.Distance(point, current.transform.position);   

        if(playerDistanceToPoint<3f)
        {
            score -= 100f;
        }
        else if(playerDistanceToPoint<8f)
        {
            score += 30f;
        }
            foreach (EnemyAI enemy in enemies)
            {
                if (enemy == null)
                    continue;

                float distance = Vector3.Distance(point, enemy.transform.position);

                //敵から離れているほど高評価
                score += distance;
            }
        foreach(TeammateAI mate in teammates)
        {
            if (mate == this)
                continue;

            float distance=Vector3.Distance(point,mate.transform.position);

            if(distance<3f)
            {
                score -= 50f;
            }
        }
        //パスコースが通か評価
        bool blocked = false;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == this) continue;

            float d = DistanceToLine(
                current.transform.position,
                point,
                enemy.transform.position);

            if(d<1.5f)
            {
                blocked = true;
                break;
            }
        }
        if (!blocked)
        {
            score += 100;
        }
        else
        {
            score -= 300f;
        }
            List<float> defenderDistances = new List<float>();

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            float distance =
                Vector3.Distance(
                    enemy.transform.position,
                    opponentGoal.position);

            defenderDistances.Add(distance);
        }
        defenderDistances.Sort();
        if (defenderDistances.Count >= 2)
        {
            float secondDefenderDistance = defenderDistances[1];

            float pointDistance =
                Vector3.Distance(point, opponentGoal.position);

            float playerDistance =
                Vector3.Distance(current.transform.position, opponentGoal.position);

            if (pointDistance < playerDistance &&
                pointDistance < secondDefenderDistance)
            {
                score -= 1000f;
            }
        }
        return score;
    }
    private void AttackMove()
    {
        switch (Role)
        {
            case TeammateRole.Support:
                SupportMove();
                break;

            case TeammateRole.RunBehind:
                RunBehindMove();
                break;
        }
        
    }
    private void DefenceMove()
    {
        if(!agent.enabled) return;
        switch (Role)
        {
            case TeammateRole.CutPass:
                CutPassMove();
                break;

            case TeammateRole.CoverGoal:
                CoverGoalMove();
                break;
        }
    }
    private void SupportMove()
    {
        if (!agent.enabled)
            return;

        PlayerController current = GameManager.Instance.CurrentPlayer;

        if (current == null || opponentGoal == null)
            return;

        Vector3 goalDir =
            (opponentGoal.position - current.transform.position).normalized;
        goalDir.y = 0;

        List<Vector3> candidates = new List<Vector3>();

        // プレイヤーの周囲5～8mに候補を作る
        for (int i = 0; i < 30; i++)
        {
            Vector3 point =
                current.transform.position +
                goalDir * Random.Range(3f, 6f) +
                Random.insideUnitSphere * 3f;

            point.y = current.transform.position.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(point, out hit, 1.5f, NavMesh.AllAreas))
            {
                candidates.Add(hit.position);
            }
        }

        if (candidates.Count == 0)
            return;

        Vector3 bestTarget = candidates[0];
        float bestScore = float.MinValue;

        foreach (Vector3 point in candidates)
        {
            float score = EvaluatePositiion(point);

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = point;
            }
        }

        agent.SetDestination(bestTarget);
    }

    private void RunBehindMove()
    {
        if (!agent.enabled)
            return;

        PlayerController current = GameManager.Instance.CurrentPlayer;

        if (current == null || opponentGoal == null)
            return;

        Vector3 goalDir =
            (opponentGoal.position - current.transform.position).normalized;
        goalDir.y = 0;

        // プレイヤーより前に8m
        Vector3 target =
            current.transform.position +
            goalDir * 8f;

        // 一番近い敵から少し逃げる
        EnemyAI nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            float d =
                Vector3.Distance(target, enemy.transform.position);

            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null)
        {
            Vector3 away =
                (target - nearestEnemy.transform.position).normalized;

            target += away * 3f;
        }

        NavMeshHit hit;

        if (NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    private void CutPassMove()
    {
        BallController ball = FindAnyObjectByType<BallController>();

        if (ball == null)
            return;
        //パス中ならインターセプト
        if(ball.IsPassing&&isReceiver)
        {
            InterceptBall(ball);
            return;
        }
        EnemyAI owner = ball.Owner as EnemyAI;

        // 敵がボールを持っていなければ何もしない
        if (owner == null)
            return;

        EnemyAI receiver = null;
        float bestScore = float.MinValue;

        // 一番危険なパス相手を探す
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            if (enemy == owner)
                continue;

            float score = 0;

            // ① ゴールに近い敵ほど高評価
            score -= Vector3.Distance(
                enemy.transform.position,
                ownGoal.position);

            // ② フリーな敵ほど高評価
            float nearestMate = float.MaxValue;

            foreach (TeammateAI mate in teammates)
            {
                if (mate == this)
                    continue;

                float d = Vector3.Distance(
                    enemy.transform.position,
                    mate.transform.position);

                if (d < nearestMate)
                    nearestMate = d;
            }

            score += nearestMate;

            // ③ ボール保持者が向いている方向なら高評価
            Vector3 dir =
                (enemy.transform.position - owner.transform.position).normalized;

            float dot =
                Vector3.Dot(owner.transform.forward, dir);

            score += dot * 20f;

            // 一番評価が高い敵を保存
            if (score > bestScore)
            {
                bestScore = score;
                receiver = enemy;
            }
        }

        // パスコースを塞ぐ位置へ移動
        if (receiver != null)
        {
            Vector3 target =
                Vector3.Lerp(
                    owner.transform.position,
                    receiver.transform.position,
                    0.7f);

            agent.SetDestination(target);
        }
        //インターセプト
        float distance=
            Vector3.Distance(transform.position,ball.transform.position);

        if(distance< interceptDistance)
        {
            if(ball.CanSteal&&ball.Owner!=this)
            {
               TakeBall(ball);
            }
        }
    }
    private void CoverGoalMove()
    {
        BallController ball = FindAnyObjectByType<BallController>();

        if (ball == null)
            return;

        Vector3 target =
            Vector3.Lerp(
                ownGoal.position,
                ball.transform.position,
                0.3f);

        agent.SetDestination(target);
    }

    private float DistanceToLine(
        Vector3 start,
        Vector3 end,
        Vector3 point)
    {
        Vector3 line = end - start;

        float t =
            Mathf.Clamp01(
                Vector3.Dot(point - start, line) /
                line.sqrMagnitude);

        Vector3 closest = start + line * t;

        return Vector3.Distance(point,closest);
    }
    private void InterceptBall(BallController ball)
    {
        if (!agent.enabled) return;
        Rigidbody rb=ball.GetComponent<Rigidbody>();

        Vector3 target=ball.transform.position+rb.linearVelocity.normalized*0.3f;

        agent.SetDestination(target);

        float distance = Vector3.Distance(transform.position, ball.transform.position);

        if (distance < 2f&&ball.Owner==null)
        {
           TakeBall(ball);
        }
    }
    private void TakeBall(BallController ball)
    {
        ball.SetOwner(this);
        currentBall = ball;
        isReceiver = false;
    }
}
