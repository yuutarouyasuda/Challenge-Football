using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class KickInManager : MonoBehaviour
{
    public static KickInManager Instance;

    [SerializeField] private BallController ball;
    [SerializeField] private float kickPower = 12f;
    private void Awake()
    {
        Instance = this;
    }


    public void StartKickIn()
    {
        Debug.Log("キックイン開始");

        MonoBehaviour last = ball.LastOwner;

        Debug.Log("LastKicker : " + last);
        if (last == null)
            return;


        // 最後に触ったのが味方
        // → 敵キックイン
        if (last.GetComponent<PlayerController>() != null)
        {
            EnemyAI enemy = FindNearestEnemy();

            if (enemy != null)
            {
                Debug.Log("敵キックイン選手 : " + enemy.name);

                StartCoroutine(SetKickInPlayer(enemy));
            }
        }


        // 最後に触ったのが敵
        // → 味方キックイン
        else if (last.GetComponent<EnemyAI>() != null)
        {
            PlayerController player = FindNearestPlayer();

            if (player != null)
            {
                Debug.Log("味方キックイン選手 : " + player.name);

                StartCoroutine(SetKickInPlayer(player));
            }
        }
    }
    private PlayerController FindNearestPlayer()
    {
        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        PlayerController nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (PlayerController player in players)
        {
            float distance =
                Vector3.Distance(
                    ball.transform.position,
                    player.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = player;
            }
        }

        return nearest;
    }
    private EnemyAI FindNearestEnemy()
    {
        EnemyAI[] enemies =
            FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        EnemyAI nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (EnemyAI enemy in enemies)
        {
            float distance =
                Vector3.Distance(
                    ball.transform.position,
                    enemy.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    private IEnumerator SetKickInPlayer(MonoBehaviour player)
    {
        yield return new WaitForSeconds(0.5f);

        NavMeshAgent agent =
            player.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.Warp(ball.transform.position);
        }
        else
        {
            player.transform.position =
                ball.transform.position;
        }

        ball.SetOwner(player);

        StartCoroutine(KickInPass(player));
    }
    private IEnumerator KickInPass(MonoBehaviour player)
    {
        // 少し待ってから蹴る
        yield return new WaitForSeconds(1f);


        Transform target = FindKickTarget(player);

        if (target == null)
        {
            Debug.Log("キック先なし");
            yield break;
        }


        Vector3 dir =
            (target.position - ball.transform.position).normalized;


        ball.Kick(
            dir,
            kickPower,
            false,
            target.GetComponent<MonoBehaviour>()
        );


        Debug.Log(
            player.name + " → " + target.name + "へキックイン"
        );
    }
    private Transform FindKickTarget(MonoBehaviour kicker)
    {
        float minDistance = Mathf.Infinity;
        Transform target = null;


        // プレイヤーが蹴る場合
        if (kicker.GetComponent<PlayerController>() != null)
        {
            EnemyAI[] enemies =
                FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

            // 自分のチームはPlayerControllerなので、
            // 味方(PlayerController)を探す
            PlayerController[] players =
                FindObjectsByType<PlayerController>(FindObjectsSortMode.None);


            foreach (PlayerController player in players)
            {
                if (player.gameObject == kicker.gameObject)
                    continue;


                float distance =
                    Vector3.Distance(
                        kicker.transform.position,
                        player.transform.position);


                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = player.transform;
                }
            }
        }


        // 敵AIが蹴る場合
        else if (kicker.GetComponent<EnemyAI>() != null)
        {
            EnemyAI[] enemies =
                FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);


            foreach (EnemyAI enemy in enemies)
            {
                if (enemy.gameObject == kicker.gameObject)
                    continue;


                float distance =
                    Vector3.Distance(
                        kicker.transform.position,
                        enemy.transform.position);


                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = enemy.transform;
                }
            }
        }


        return target;
    }
}