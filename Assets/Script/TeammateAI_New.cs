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
    [Header("Tackle")]
    [SerializeField] private float tackleRange = 1.5f;
    [SerializeField] private float tackleAngle = 60f;
    [SerializeField] private float tackleCooldown = 1f;
    [SerializeField] private float tackleDash = 2f;

    private float tackleTimer;
    private Rigidbody rb;
    private BallController currentBall;
    public TeammateRole Role { get; set; }
    public bool isReceiver = false;

    private NavMeshAgent agent;
    private PlayerData playerData;
    private PlayerController player;

    public void SetPlayerData(PlayerData data)
    {
        playerData = data;

        Debug.Log(
            name + " : " +
            playerData.playerName +
            " Speed:" + playerData.speed +
            " Kick:" + playerData.kick +
            " Dribble:" + playerData.dribble +
            " Defence:" + playerData.defence);
    }
    public PlayerData GetPlayerData()
    {
        return playerData;
    }
    private void Awake()
    {
        player =GetComponent<PlayerController>();

        agent = GetComponent<NavMeshAgent>();

        rb = GetComponent<Rigidbody>();

        teammates = FindObjectsByType<TeammateAI_New>(FindObjectsSortMode.None);
        enemies=FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
    }

    private float MoveSpeed
    {
        get
        {
            if (playerData == null)
                return 4f;

            return Mathf.Lerp(
                3f,
                6f,
                playerData.speed / 100f);
        }
    }
    private float KickPower
    {
        get
        {
            if (playerData == null)
                return 20f;

            return Mathf.Lerp(
                10f,
                30f,
                playerData.kick / 100f);
        }
    }
    private bool initialized = false;
    public void Initialize()
    {
        initialized = true;
    }
    private void Update()
    {
        if (GameManager.Instance.IskickIn)
        {
            agent.ResetPath();
            return;
        }
        if (supportTimer > 0f)
        {
            supportTimer -= Time.deltaTime;
        }
        if (GameManager.Instance.IsGameStop)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        agent.speed = MoveSpeed;
        if (tackleTimer > 0)
        {
            tackleTimer-= Time.deltaTime;   
        }
        if (GameManager.Instance.IsKickOff ||
    GameManager.Instance.IsGameStop)
        {
            agent.ResetPath();
            return;
        }
        if (initialized)
            return;
        BallController ball = FindAnyObjectByType<BallController>();
        if (ball != null && ball.Owner == null)
        {
            ChaseLooseBall(ball);
            return;
        }
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
       /* Debug.Log(
    name +
    " speed=" + agent.speed +
    " velocity=" + agent.velocity.magnitude
);*/
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

        if (nearestEnemy < 2f)
        {
            score -= 300f;
        }
        else if (nearestEnemy < 4f)
        {
            score -= 150f;
        }
        else
        {
            score += nearestEnemy * 20f;
        }
        int nearbyEnemyCount = 0;

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null)
                continue;

            if (Vector3.Distance(point, enemy.transform.position) < 3f)
            {
                nearbyEnemyCount++;
            }
        }

        score -= nearbyEnemyCount * 120f;
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
        if (goalDistance < 15f &&
    nearestEnemy > 5f)
        {
            score += 200f;
        }
        //フィールドの端行きにくくする
        //float fieldHalfWidth = 20f;

        //if (Mathf.Abs(point.x) > fieldHalfWidth - 2f)
        //{
        //    score -= 100f;
        //}
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
        if (!agent.enabled)
            return;

        PlayerController owner = GameManager.Instance.CurrentPlayer;

        if (owner == null)
            return;

        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;


        Debug.Log(playerData);
        float Maxradius =
            Mathf.Lerp(
                6f,
                10f,
                playerData.speed / 100f);
     // 半径6m・8m・10mを調べる
for (float radius = 6f; radius <= 10f; radius += 2f)
{
    for (int i = 0; i < 16; i++)
    {
        float angle = i * 22.5f;

        Vector3 dir =
            Quaternion.Euler(0, angle, 0) *
            owner.transform.forward;

        Vector3 candidate =
            owner.transform.position +
            dir * radius;

        NavMeshHit hit;

        if (!NavMesh.SamplePosition(
            candidate,
            out hit,
            2f,
            NavMesh.AllAreas))
        {
            continue;
        }

        float score = EvaluatePosition(hit.position);

        if (score > bestScore)
        {
            bestScore = score;
            bestPoint = hit.position;
        }
    }
}

        if (supportTimer <= 0f)
        {
            supportTimer = 0.5f;

            agent.SetDestination(bestPoint);
        }
    }
    private void RunBehindMove()
    {
        if (!agent.enabled)
            return;

        EnemyAI nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (EnemyAI enemy in enemies)
        {
            float d =
                Vector3.Distance(transform.position,
                                 enemy.transform.position);

            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearest = enemy;
            }
        }

        if (nearest == null)
            return;
        float runDistance =
            Mathf.Lerp(
                4f,
                8f,
                playerData.speed / 100f);

        Vector3 goalDir =
            (opponentGoal.position -
             nearest.transform.position).normalized;

        Vector3 target =
            nearest.transform.position +
            goalDir * runDistance;
        Vector3 side =
    Vector3.Cross(Vector3.up, goalDir);

        target += side * Random.Range(-2f, 2f);
        NavMeshHit hit;
        if (Vector3.Distance(target, nearest.transform.position) < 2f)
        {
            target += goalDir * 2f;
        }
        if (NavMesh.SamplePosition(target,
                                   out hit,
                                   2f,
                                   NavMesh.AllAreas))
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
        Vector3 target =
            owner.transform.position +
            owner.transform.forward * 1.5f;

        agent.SetDestination(target);
        float distance =
    Vector3.Distance(
        transform.position,
        owner.transform.position);

        if (distance < tackleRange)
        {
            TryTackle(ball);
        }
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
        // すでに誰かが持っているなら取らない
        if (ball.Owner != null)
            return;

        if (!ball.CanSteal) return;

        ball.SetOwner(player);

        isReceiver = false;
    }
    private void ChaseLooseBall(BallController ball)
    {
        if (ball.IsPassing) return;

        if (!ball.CanSteal) return;

        // 自分が一番近いか判定
        float myDistance =
            Vector3.Distance(transform.position, ball.transform.position);

        foreach (TeammateAI_New mate in teammates)
        {
            if (mate == null || mate == this)
                continue;

            float d =
                Vector3.Distance(
                    mate.transform.position,
                    ball.transform.position);

            if (d < myDistance)
            {
                // 自分より近い味方がいる
                return;
            }
        }
        if (!agent.isOnNavMesh)
            return;
        // 自分が一番近いので取りに行く
        agent.SetDestination(ball.transform.position);

        if (myDistance < 2f)
        {
            TakeBall(ball);
        }
    }
    private void TryTackle(BallController ball)
    {
        if (tackleTimer > 0f)
            return;

        if (ball.Owner == null)
            return;

        // 味方からは奪わない
        if (ball.Owner is PlayerController ||
            ball.Owner is TeammateAI_New)
            return;

        Vector3 dir =
            ball.Owner.transform.position - transform.position;

        dir.y = 0;

        float angle =
            Vector3.Angle(transform.forward, dir);
        if (angle > tackleAngle)
            return;


        float success =
            Mathf.Lerp(
                0.4f,
                0.95f,
                playerData.defence / 100f);


        if (Random.value > success)
        {
            Debug.Log(name + " タックル失敗");
            return;
        }


        tackleTimer = tackleCooldown;

        rb.AddForce(
            transform.forward * tackleDash,
            ForceMode.VelocityChange);

        ball.SetOwner(player);
        currentBall = ball;

        Debug.Log(name + " タックル成功");
    }
}