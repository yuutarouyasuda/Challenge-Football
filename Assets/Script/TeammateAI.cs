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
    }

    public TeammateRole Role
    {
        get;
        set;
    }
    [SerializeField] private Transform ownGoal;
    [Header("Reference")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform opponentGoal;
    [SerializeField] private EnemyAI[] enemies;
    [SerializeField] private TeammateAI[] teammates;
    [Header("Move")]
    [SerializeField] private float forwardDistance = 6f;
    [SerializeField] private float sideDistance = 4f;
    [SerializeField] private float avoidRadius=2f;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent=GetComponent<NavMeshAgent>();
    }
    
    void Update()
    {
        bool isAttack = false;

        BallController ballController = FindFirstObjectByType<BallController>();

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
        score -= Vector3.Distance(point, opponentGoal.position);

        foreach(EnemyAI enemy in enemies)
        {
            if (enemy == null)
            continue;

            float distance=Vector3.Distance(point,enemy.transform.position);

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
                player.position,
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
                Vector3.Distance(player.position, opponentGoal.position);

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
        if (player == null || opponentGoal == null)
            return;

        //プレイヤーの味方
        Vector3 goalDir = (opponentGoal.position - player.position).normalized;
        goalDir.y = 0;

        //候補地点
        List<Vector3> candidates = new List<Vector3>();

        for (int i = 0; i < 30; i++)
        {
            Vector2 random = Random.insideUnitCircle * 8f;

            Vector3 point =
                player.position +
                goalDir * forwardDistance +
                new Vector3(random.x, 0, random.y);

            //フィールド外なら候補にしない
            if (point.x < -50 || point.x > 50)
                continue;

            if (point.z < -50 || point.z > 50)
                continue;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(point, out hit, 1.5f, NavMesh.AllAreas))
            {
                candidates.Add(hit.position);
            }
        }
        Vector3 bestTarget = candidates[0];
        float bestScore = -999f;

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
    private void DefenceMove()
    {
        Debug.Log(Role);
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
    private void CutPassMove()
    {
        BallController ball = FindAnyObjectByType<BallController>();

        if (ball == null)
            return;
        //パス中ならインターセプト
        if(ball.IsPassing)
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
        Rigidbody rb=ball.GetComponent<Rigidbody>();

        Vector3 target=ball.transform.position+rb.linearVelocity.normalized*2f;

        agent.SetDestination(target);
    }
}
