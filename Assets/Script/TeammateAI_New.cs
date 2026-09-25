using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

[RequireComponent(typeof(NavMeshAgent))]
public class TeammateAI_New : MonoBehaviour
{
    public enum TeammateRole
    {
        Support,
        RunBehind,
        Press,
        Cover
    }
    [Header("Reference")]
    [SerializeField] private Transform ownGoal;
    [SerializeField] private Transform opponentGoal;
    [SerializeField] private EnemyAI[] enemies;
    [SerializeField]private TeammateAI_New[] teammates;
    public TeammateRole Role { get; set; }
    public bool isReceiver = false;

    private NavMeshAgent agent;
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        teammates=FindObjectsByType<TeammateAI_New>(FindObjectsSortMode.None);
        enemies=FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        BallController ball = FindAnyObjectByType<BallController>();

        if (isReceiver && ball != null && ball.Owner == null)
        {
            Vector3 target;

            Rigidbody ballRb = ball.GetComponent<Rigidbody>();

            if (ballRb != null && ballRb.linearVelocity.y > 1f)
            {
                // ロブパスなら着地点へ
                target = PredictLandingPoint(ball);
            }
            else
            {
                // グラウンダーパスなら少し先を追う
                target = ball.transform.position;

                if (ballRb != null)
                {
                    target += ballRb.linearVelocity * 0.3f;
                }

                target.y = transform.position.y;
            }

            agent.SetDestination(target);

            if (Vector3.Distance(transform.position, ball.transform.position) < 2f)
            {
                TakeBall(ball);
                return;
            }
        }

        // 守備時だけルーズボールを拾う
        if (Role == TeammateRole.Press)
        {
            if (ball != null &&
                ball.Owner == null &&
                Vector3.Distance(transform.position, ball.transform.position) < 2f)
            {
                TakeBall(ball);
                return;
            }
        }

        switch (Role)
        {
            case TeammateRole.Support:
                SupportMove();
                break;

            case TeammateRole.RunBehind:
                RunBehindMove();
                break;

            case TeammateRole.Press:
                PressMove();
                break;

            case TeammateRole.Cover:
                CoverMove();
                break;
        }
    }

    private float EvaluatePosition(Vector3 point)
    {
        float score = 0f;

        PlayerController current = GameManager.Instance.CurrentPlayer;

        if (current == null)
            return float.MinValue;

        //-------------------------------------------------
        // ① ゴールに近いほど高評価
        //-------------------------------------------------
        float goalDistance =
            Vector3.Distance(point, opponentGoal.position);

        score -= goalDistance;

        // ゴールに近すぎる場所は減点
        if (goalDistance < 6f)
        {
            score -= 150f;
        }

        //-------------------------------------------------
        // ② プレイヤーと近すぎる場所は減点
        //-------------------------------------------------
        float playerDistance =
            Vector3.Distance(point, current.transform.position);

        if (playerDistance < 3f)
        {
            score -= 100f;
        }

        //-------------------------------------------------
        // ③ プレイヤーの前にいるほど高評価
        //-------------------------------------------------
        Vector3 toPoint =
            (point - current.transform.position).normalized;

        float forward =
            Vector3.Dot(current.transform.forward, toPoint);

        score += forward * 80f;

        //-------------------------------------------------
        // ④ 一番近い敵との距離
        //-------------------------------------------------
        float nearestEnemy = Mathf.Infinity;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            float d =
                Vector3.Distance(point, enemy.transform.position);

            if (d < nearestEnemy)
            {
                nearestEnemy = d;
            }
        }

        score += nearestEnemy * 25f;

        //-------------------------------------------------
        // ⑤ 味方と近すぎる場所は減点
        //-------------------------------------------------
        foreach (TeammateAI_New mate in teammates)
        {
            if (mate == null || mate == this)
                continue;

            float d =
                Vector3.Distance(point, mate.transform.position);

            if (d < 3f)
            {
                score -= 80f;
            }
        }

        //-------------------------------------------------
        // ⑥ パスコースが塞がれていないか
        //-------------------------------------------------
        bool blocked = false;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            float d =
                DistanceToLine(
                    current.transform.position,
                    point,
                    enemy.transform.position);

            if (d < 1.5f)
            {
                blocked = true;
                break;
            }
        }

        if (blocked)
        {
            score -= 200f;
        }
        else
        {
            score += 100f;
        }

        //-------------------------------------------------
        // ⑦ ゴール方向へ向きやすい位置
        //-------------------------------------------------
        Vector3 goalDir =
            (opponentGoal.position - point).normalized;

        float attackAngle =
            Vector3.Dot(toPoint, goalDir);

        score += attackAngle * 60f;

        //-------------------------------------------------
        // ⑧ 今いる位置から近いほど少し高評価
        // （フラフラ防止）
        //-------------------------------------------------
        float moveDistance =
            Vector3.Distance(transform.position, point);

        score -= moveDistance * 3f;

        return score;
    }
    private float supportTimer;
    private void SupportMove()
    {
        supportTimer -= Time.deltaTime;

        if (supportTimer > 0f)
            return;

        supportTimer = 0.5f;
        if (!agent.enabled)
            return;

        PlayerController current = GameManager.Instance.CurrentPlayer;

        if (current == null || opponentGoal == null)
            return;

        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;

        // 半径
        float radius = 12f;

        // 候補数
        int sampleCount = 80;

        for (int i = 0; i < sampleCount; i++)
        {
            // ランダム方向
            Vector2 random =
                Random.insideUnitCircle * radius;

            Vector3 point =
                current.transform.position +
                new Vector3(random.x, 0, random.y);

            NavMeshHit hit;

            if (!NavMesh.SamplePosition(point, out hit, 2f, NavMesh.AllAreas))
                continue;

            float score = EvaluatePosition(hit.position);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = hit.position;
            }
        }

        // 少ししか変わらないなら更新しない
        if (Vector3.Distance(agent.destination, bestPoint) > 1f)
        {
            agent.SetDestination(bestPoint);
        }
    }
    private void RunBehindMove()
    {
        EnemyAI lastDefender = null;
        float best = float.MaxValue;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            float d =
                Vector3.Distance(enemy.transform.position,
                                 opponentGoal.position);

            if (d < best)
            {
                best = d;
                lastDefender = enemy;
            }
        }
        if (lastDefender == null)
            return;

        Vector3 goalDir =
            (opponentGoal.position - lastDefender.transform.position).normalized;

        Vector3 target =
            lastDefender.transform.position +
            goalDir * 3f;
        NavMeshHit hit;

        if (NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void PressMove()
    {
        if (!agent.enabled)
            return;

        BallController ball = FindFirstObjectByType<BallController>();

        if (ball == null)
            return;

        EnemyAI owner = ball.Owner as EnemyAI;

        if (owner == null)
            return;

        agent.SetDestination(owner.transform.position);
    }

    private void CoverMove()
    {
        if (!agent.enabled)
            return;

        BallController ball = FindFirstObjectByType<BallController>();

        if (ball == null)
            return;

        Vector3 target = Vector3.Lerp(
            ownGoal.position,
            ball.transform.position,
            0.3f);

        agent.SetDestination(target);
    }

    private float DistanceToLine(Vector3 start,Vector3 end,Vector3 point)
    {
        Vector3 line = end - start;

        float t = Mathf.Clamp01(Vector3.Dot(point - start, line) / line.sqrMagnitude);

        Vector3 closest = start + line * t;

        return Vector3.Distance(point, closest);    
    }
    private Vector3 PredictLandingPoint(BallController ball)
    {
        Rigidbody rb = ball.GetComponent<Rigidbody>();

        if (rb == null)
            return ball.transform.position;

        Vector3 position = rb.position;
        Vector3 velocity = rb.linearVelocity;

        float gravity = Mathf.Abs(Physics.gravity.y);

        // y=0(地面)まで落ちる時間を計算
        float a = -0.5f * gravity;
        float b = velocity.y;
        float c = position.y;

        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
            return position;

        float t =
            (-b - Mathf.Sqrt(discriminant)) / (2 * a);

        if (t < 0)
        {
            t = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        }

        Vector3 landing =
            position + new Vector3(velocity.x, 0, velocity.z) * t;

        landing.y = transform.position.y;

        return landing;
    }
    private void TakeBall(BallController ball)
    {
        PlayerController player = GetComponent<PlayerController>();

        ball.SetOwner(player);

        isReceiver = false;
    }
}