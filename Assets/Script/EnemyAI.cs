using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum DefenderRole
    {
        Press,
        CutPass,
        CoverGoal,
        Support,
        RunBehind
    }

    private enum ActionType
    {
        Shoot,
        Pass,
        Dribble,
        Escape,
        Keep
    }


    public DefenderRole Role { get; set; }
    [SerializeField] private Transform ball;
    [SerializeField] private Transform opponentGoal;
    [SerializeField] private Transform ownGoal;
    [SerializeField] private bool canShoot = false;
    [SerializeField] private EnemyAI[] teammates;
    [SerializeField] private float kickOffPassPower = 20f;
    [SerializeField] private EnemyAI kickOffReceiver;
    [SerializeField, Range(1, 100)]
    private int dribble = 50;
    public float Dribble => dribble;
    private EnemyManager enemyManager;
    private float passTimer = 0f;
    private BallController currentBall;
    private NavMeshAgent agent;
    private float noPickupTimer = 0f;
    private bool isEscaping = false;
    private Vector3 escapeTarget;
    private float escapePassBonusTimer = 0f;
    public bool isReceiver;
    private bool isPassing;
    private float bestPassScore;
    private float afterKickMoveTimer = 0f;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }
    public void ResetPosition()
    {
        transform.position = startPosition;

        if (GetComponent<NavMeshAgent>() != null)
        {
            GetComponent<NavMeshAgent>().ResetPath();
        }
    }
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        enemyManager = FindFirstObjectByType<EnemyManager>();
    }
    private void Update()
    {
        if(isKickOffPlayer)
        {
            KickOffPass();
            return;
        }
        if (GameManager.Instance.IsKickOff ||
    GameManager.Instance.IsGameStop)
        {
            agent.ResetPath();
            return;
        }

        PlayerController[] players =
    FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        agent.speed = enemyManager.moveSpeed;
        if (escapePassBonusTimer > 0f)
        {
            escapePassBonusTimer -= Time.deltaTime;
        }
        if (noPickupTimer>0f)
        {
            noPickupTimer -= Time.deltaTime;
        }
        if (afterKickMoveTimer > 0f)
        {
            afterKickMoveTimer -= Time.deltaTime;
        }
        if (ball == null)
            return;

        BallController ballController = ball.GetComponent<BallController>();

        if (isReceiver && ballController.Owner == null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            Vector3 target = ball.position;

            if (rb != null)
            {
                target += rb.linearVelocity * 0.8f;
                target.y = transform.position.y;
            }

            agent.SetDestination(target);
            if (Vector3.Distance(transform.position, ball.position) < 1.2f)
            {
                ballController.SetOwner(this);
                currentBall = ballController;
                isReceiver = false;
            }

            return;
        }
        if (isEscaping)
        {
            if (!agent.pathPending && agent.remainingDistance < 0.3f)
            {
                isEscaping = false;
                escapePassBonusTimer = 2f;
            }

            return;
        }
        //ボールを失ったら所有権を失う
        if (currentBall != null && ballController.Owner != this)
        {
            currentBall = null;
        }

        // パス・シュート直後はボールを追わない
        if (afterKickMoveTimer > 0f)
        {
            AttackSupportMove(ballController);
            return;
        }

        // ボールを持っていない
        if (currentBall == null)
        {
            // 味方がボールを持っている
            if (ballController.Owner is EnemyAI)
            {
                AttackSupportMove(ballController);
            }
            else
            {
                // 相手がボールを持っている
                switch (Role)
                {
                    case DefenderRole.Press:
                        PressMove(ballController);
                        break;

                    case DefenderRole.CutPass:
                        CutPassMove(ballController);
                        break;

                    case DefenderRole.CoverGoal:
                        CoverMove(ballController);
                        break;
                }
            }
        }
        // ボールを持っている
        else
        {
            passTimer -= Time.deltaTime;
            //Debug.Log(passTimer);   
            float distance = Vector3.Distance(
                transform.position,
                opponentGoal.position);
            EnemyAI receiver = FindBestReceiver();

            float shootScore = EvaluateShoot();
            float passScore = bestPassScore;
            if (escapePassBonusTimer > 0f)
            {
                passScore += 100f;
            }
            float dribbleScore = EvaluateDribble();
            float escapeScore=EvaluateEscape();
            float keepScore = EvaluateKeep();
            ActionType action = ActionType.Dribble;
            float bestScore = dribbleScore;

            if (passScore > bestScore)
            {
                bestScore = passScore;
                action = ActionType.Pass;
            }

            if (shootScore > bestScore)
            {
                bestScore = shootScore;
                action = ActionType.Shoot;
            }
            if (escapeScore > bestScore)
            {
                bestScore = escapeScore;
                action = ActionType.Escape;
            }
            if (keepScore > bestScore)
            {
                bestScore = keepScore;
                action = ActionType.Keep;
            }
            /*Debug.Log(
    $"Shoot:{shootScore} " +
    $"Pass:{passScore} " +
    $"Dribble:{dribbleScore} " +
    $"Escape:{escapeScore} " +
    $"Keep:{keepScore} " +
    $"=> {action}");*/
            switch (action)
            {
                case ActionType.Shoot:

                    Vector3 shootDir =
                        opponentGoal.position - transform.position;

                    shootDir.y = 0;

                    transform.forward = shootDir.normalized;

                    currentBall.Kick(
                        shootDir.normalized,
                        enemyManager.shootPower,
                        false);
                  
                    currentBall = null;
                    noPickupTimer = 0.5f;
                    afterKickMoveTimer = 0.5f;
                    break;

                case ActionType.Pass:

                    if (receiver != null &&
                       passTimer <= 0f &&
                       !isPassing)
                    {
                        StartCoroutine(PassBallCoroutine());
                        passTimer = enemyManager.passCooldown;
                    }

                    break;

                case ActionType.Dribble:

                    agent.SetDestination(opponentGoal.position);

                    break;
                case ActionType.Escape:

                    if (!isEscaping)
                    {
                        Vector3 left = -transform.right * 5f;
                        Vector3 right = transform.right * 5f;

                        Vector3 leftPos = transform.position + left;
                        Vector3 rightPos = transform.position + right;

                        float leftScore = 0f;
                        float rightScore = 0f;

                        foreach (PlayerController player in players)
                        {
                            leftScore += Vector3.Distance(leftPos, player.transform.position);
                            rightScore += Vector3.Distance(rightPos, player.transform.position);
                        }

                        escapeTarget = (leftScore > rightScore) ? leftPos : rightPos;

                        agent.SetDestination(escapeTarget);
                        isEscaping = true;
                    }

                    break;
                case ActionType.Keep:

                    agent.ResetPath();

                    PlayerController nearest = null;
                    float nearestDistance = Mathf.Infinity;

                    foreach (PlayerController player in players)
                    {
                        float d = Vector3.Distance(
                            transform.position,
                            player.transform.position);

                        if (d < nearestDistance)
                        {
                            nearestDistance = d;
                            nearest = player;
                        }
                    }

                    if (nearest != null)
                    {
                        Vector3 dir =
                            transform.position - nearest.transform.position;

                        dir.y = 0;

                        transform.forward = dir.normalized;
                    }

                    break;
            }
        }
    }
    private EnemyAI FindKickOffReceiver()
    {
        EnemyAI nearest = null;
        float distance = Mathf.Infinity;

        foreach (EnemyAI mate in teammates)
        {
            if (mate == null || mate == this)
                continue;

            float d = Vector3.Distance(
                transform.position,
                mate.transform.position);

            if (d < distance)
            {
                distance = d;
                nearest = mate;
            }
        }

        return nearest;
    }
    private bool isKickOffPlayer = false;
public void SetKickOffPlayer()
    {
        isKickOffPlayer = true;
    }
    private void KickOffPass()
    {
        if (kickOffReceiver == null)
        {
            Debug.Log("キックオフ受け手なし");
            return;
        }


        BallController ball =
            FindAnyObjectByType<BallController>();


        Vector3 dir =
            kickOffReceiver.transform.position
            - transform.position;


        dir.y = 0;


        ball.Kick(
            dir.normalized,
            kickOffPassPower,
            false
        );


        Debug.Log(
            name + " → " +
            kickOffReceiver.name +
            "へキックオフパス"
        );


        isKickOffPlayer = false;


        KickOffManager kickOff =
            FindAnyObjectByType<KickOffManager>();

        if (kickOff != null)
        {
            kickOff.EndKickOff();
        }
    }
    private EnemyAI FindBestReceiver()
    {
        EnemyAI best = null;
        float bestScore = float.MinValue;

        foreach (EnemyAI mate in teammates)
        {
            if (mate == null || mate == this)
                continue;

            float score = 0f;

            //----------------------------------
            // ① ゴールに近いほど加点
            //----------------------------------
            float goalDistance =
                Vector3.Distance(mate.transform.position, opponentGoal.position);

            score -= goalDistance;

            //----------------------------------
            // ② 近すぎる味方は減点
            //----------------------------------
            float myDistance =
                Vector3.Distance(transform.position, mate.transform.position);

            if (myDistance < 4f)
                score -= 80f;

            //----------------------------------
            // ③ 遠すぎる味方も少し減点
            //----------------------------------
            if (myDistance > 20f)
                score -= 30f;

            //----------------------------------
            // ④ 敵から離れているほど加点
            //----------------------------------
            PlayerController[] players =
                FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            float nearestEnemy = Mathf.Infinity;

            foreach (PlayerController player in players)
            {
                float d = Vector3.Distance(
                    mate.transform.position,
                    player.transform.position);

                if (d < nearestEnemy)
                    nearestEnemy = d;
            }

            score += nearestEnemy * 5f;

            //----------------------------------
            // ⑤ パスコースが通るか
            //----------------------------------
            bool blocked = false;

            foreach (PlayerController player in players)
            {
                float d = DistanceToLine(
                    transform.position,
                    mate.transform.position,
                    player.transform.position);

                if (d < 1.5f)
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked)
                score -= 80f;
            else
                score += 30f;

            //----------------------------------
            // 一番高い人を選ぶ
            //----------------------------------
            if (score > bestScore)
            {
                bestScore = score;
                best = mate;
            }
        }
        bestPassScore = bestScore;
        return best;
    }
    private void PressMove(BallController ballController)
    {
        Vector3 target;

        // ボールを誰かが持っている
        if (ballController.Owner != null)
        {
            Rigidbody ownerRb =
                ballController.Owner.GetComponent<Rigidbody>();

            // 所持者の少し先を予測
            target = ballController.Owner.transform.position;

            if (ownerRb != null)
            {
                target += ownerRb.linearVelocity * 0.5f;
            }

            // ゴール方向へ追い込む
            Vector3 goalDir =
                (ownGoal.position - ballController.Owner.transform.position).normalized;

            target += goalDir * 1.5f;
        }
        else
        {
            // ルーズボール
            target = ball.position;
        }

        agent.SetDestination(target);

        float distance =
            Vector3.Distance(transform.position, ball.position);

        if (distance < 1.2f)
        {

            if (ballController.LastKicker == this)
                return;
            if (noPickupTimer <= 0f &&
                ballController.CanSteal &&
                ballController.Owner != this)
            {
                ballController.SetOwner(this);
                currentBall = ballController;
                passTimer = enemyManager.passCooldown;
            }
        }
    }
    private void CutPassMove(BallController ballController)
    {
        if (ballController.Owner == null)
            return;

        PlayerController owner =
            ballController.Owner.GetComponent<PlayerController>();

        if (owner == null)
            return;

        // 一番ゴールに近い味方を探す
        TeammateAI_New[] teammates =
            FindObjectsByType<TeammateAI_New>(FindObjectsSortMode.None);

        PlayerController targetPlayer = null;
        float best = float.MaxValue;

        foreach (TeammateAI_New mate in teammates)
        {
            PlayerController player =
                mate.GetComponent<PlayerController>();

            if (player == owner)
                continue;

            float d =
                Vector3.Distance(
                    player.transform.position,
                    opponentGoal.position);

            if (d < best)
            {
                best = d;
                targetPlayer = player;
            }
        }

        if (targetPlayer == null)
            return;

        // パスコース上へ移動
        Vector3 target =
            Vector3.Lerp(
                owner.transform.position,
                targetPlayer.transform.position,
                0.5f);

        agent.SetDestination(target);
    }
    private void CoverMove(BallController ballController)
    {
        if (!agent.enabled)
            return;

        Vector3 goalPos = ownGoal.position;
        Vector3 ballPos = ball.position;

        // ボールとゴールの間
        Vector3 target =
            Vector3.Lerp(goalPos, ballPos, 0.35f);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    private IEnumerator PassBallCoroutine()
    {
        isPassing = true;

        EnemyAI receiver = FindBestReceiver();

        if (receiver == null)
        {
            isPassing = false;
            yield break;
        }
        Vector3 dir =
            receiver.transform.position - transform.position;

        dir.y = 0;

        Quaternion targetRotation =
            Quaternion.LookRotation(dir);

        // 向き終わるまで待つ
        while (true)
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    enemyManager.rotateSpeed * Time.deltaTime);

            float angle =
                Quaternion.Angle(
                    transform.rotation,
                    targetRotation);

            if (angle < 5f)
                break;

            yield return null;
        }

        currentBall.Kick(
            dir.normalized,
            enemyManager.passPower,
            false);
        
        currentBall = null;

        noPickupTimer = 0.5f;
        afterKickMoveTimer = 0.5f;

        foreach (EnemyAI mate in teammates)
        {
            if (mate != null)
                mate.isReceiver = false;
        }

        receiver.isReceiver = true;

        isPassing = false;
    }
    private float EvaluateShoot()
    {
        if (!canShoot)
            return -9999f;

        float score = 0f;

        //----------------------------------
        // ① ゴールに近いほど高評価
        //----------------------------------
        float distance =
            Vector3.Distance(transform.position, opponentGoal.position);

        score += 80f - distance * 2f;

        //----------------------------------
        // ② ゴール前が空いている
        //----------------------------------
        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        int blockCount = 0;

        foreach (PlayerController player in players)
        {
            float d = DistanceToLine(
                transform.position,
                opponentGoal.position,
                player.transform.position);

            if (d < 1.5f)
            {
                blockCount++;
            }
        }

        if (blockCount == 0)
        {
            score += 100f;
        }
        else if (blockCount == 1)
        {
            score += 30f;   // 1人くらいならシュートを狙う
        }
        else
        {
            score -= 50f;   // 2人以上で初めて大きく減点
        }

        //----------------------------------
        // ③ ゴールにかなり近いならさらに加点
        //----------------------------------
        if (distance < 15f)
            score += 100f;

        if (distance < 8f)
            score += 250f;

        return score;
    }
    private float EvaluateDribble()
    {
        float score = 0f;

        //----------------------------------
        // ① ゴールに近いほどドリブルしたい
        //----------------------------------
        float goalDistance =
            Vector3.Distance(transform.position, opponentGoal.position);

        score += 100f - goalDistance;

        //----------------------------------
        // ② 前方に敵がいたら減点
        //----------------------------------
        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            Vector3 dir =
                player.transform.position - transform.position;

            dir.y = 0;

            float distance = dir.magnitude;

            if (distance > 5f)
                continue;

            float angle =
                Vector3.Angle(transform.forward, dir);

            if (angle < 60f)
            {
                score -= 120f;
            }
        }

        //----------------------------------
        // ③ 周囲に敵が少ないほど加点
        //----------------------------------
        int nearbyEnemy = 0;

        foreach (PlayerController player in players)
        {
            float d =
                Vector3.Distance(
                    transform.position,
                    player.transform.position);

            if (d < 6f)
                nearbyEnemy++;
        }

        score -= nearbyEnemy * 40f;

        //----------------------------------
        // ④ ゴール方向が空いている
        //----------------------------------
        Vector3 goalDir =
            (opponentGoal.position - transform.position).normalized;

        bool blocked = false;

        foreach (PlayerController player in players)
        {
            float d =
                DistanceToLine(
                    transform.position,
                    transform.position + goalDir * 8f,
                    player.transform.position);

            if (d < 1.5f)
            {
                blocked = true;
                break;
            }
        }

        if (!blocked)
            score += 80f;

        return score;
    }
    private float EvaluateEscape()
    {
        float score = 0f;

        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        //----------------------------------
        // 前方に敵がいるほど逃げたい
        //----------------------------------
        foreach (PlayerController player in players)
        {
            Vector3 dir =
                player.transform.position - transform.position;

            dir.y = 0;

            float distance = dir.magnitude;

            if (distance > 5f)
                continue;

            float angle =
                Vector3.Angle(transform.forward, dir);

            if (angle < 60f)
            {
                score += 60f;
            }
        }

        //----------------------------------
        // ゴール方向が塞がれていたら逃げたい
        //----------------------------------
        bool blocked = false;

        foreach (PlayerController player in players)
        {
            float d =
                DistanceToLine(
                    transform.position,
                    opponentGoal.position,
                    player.transform.position);

            if (d < 1.5f)
            {
                blocked = true;
                break;
            }
        }

        if (blocked)
            score += 80f;
        float goalDistance =
    Vector3.Distance(transform.position, opponentGoal.position);

        score -= Mathf.Clamp(25f - goalDistance, 0f, 25f) * 3f;
        return score;
    }
    private float EvaluateKeep()
    {
        float score = 0f;

        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        int nearbyEnemy = 0;

        foreach (PlayerController player in players)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.transform.position);

            if (distance < 4f)
                nearbyEnemy++;
        }

        // 周囲に敵が多いほどキープしたい
        score += nearbyEnemy * 60f;

        // ゴールに遠いなら無理しない
        float goalDistance =
            Vector3.Distance(transform.position, opponentGoal.position);

        score += goalDistance * 0.5f;

        return score;
    }
    private float DistanceToLine(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 line = end - start;

        float t = Mathf.Clamp01(
            Vector3.Dot(point - start, line) / line.sqrMagnitude);

        Vector3 closest = start + line * t;

        return Vector3.Distance(point, closest);
    }
    private void AttackSupportMove(BallController ballController)
    {
        EnemyAI owner = ballController.Owner as EnemyAI;

        if (owner == null)
            return;

        switch (Role)
        {
            case DefenderRole.Support:
                SupportMove(owner);
                break;

            case DefenderRole.RunBehind:
                RunBehindMove();
                break;
        }
    }
    private void SupportMove(EnemyAI owner)
    {
        if (!agent.enabled)
            return;

        Vector3 forward = owner.transform.forward;
        Vector3 right = owner.transform.right;
        Vector3 attackDir =
            (opponentGoal.position - owner.transform.position).normalized;

        Vector3[] candidates =
        {
    owner.transform.position + attackDir * 8f,
    owner.transform.position + attackDir * 6f + owner.transform.right * 5f,
    owner.transform.position + attackDir * 6f - owner.transform.right * 5f,
    owner.transform.position + attackDir * 10f,
};


        Vector3 bestPoint = transform.position;
        float bestScore = float.MinValue;

        foreach (Vector3 p in candidates)
        {
            NavMeshHit hit;

            if (!NavMesh.SamplePosition(p, out hit, 2f, NavMesh.AllAreas))
                continue;

            float score = 0;

            // ゴールへ近い
            score -= Vector3.Distance(hit.position, opponentGoal.position);

            // プレイヤーから遠い
            PlayerController[] players =
                FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            foreach (PlayerController player in players)
            {
                score += Vector3.Distance(hit.position,
                                          player.transform.position);
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = hit.position;
            }
        }

        agent.SetDestination(bestPoint);
    }
    private void RunBehindMove()
    {
        if (!agent.enabled)
            return;

        PlayerController[] defenders =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        PlayerController nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (PlayerController player in defenders)
        {
            float d =
                Vector3.Distance(transform.position,
                                 player.transform.position);

            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearest = player;
            }
        }

        if (nearest == null)
            return;

        Vector3 goalDir =
            (opponentGoal.position -
             nearest.transform.position).normalized;

        Vector3 target =
            nearest.transform.position +
            goalDir * 8f;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(target,
                                   out hit,
                                   2f,
                                   NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
}
