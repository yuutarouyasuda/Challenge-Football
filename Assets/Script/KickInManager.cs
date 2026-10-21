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
        if (GameManager.Instance.IskickIn) return;

        Debug.Log("キックイン開始");

        GameManager.Instance.StartKickIn();

        Rigidbody rb = ball.GetComponent < Rigidbody >();
        rb.linearVelocity=Vector3.zero;
        rb.angularVelocity=Vector3.zero;

        StartCoroutine(KickInDelay());
    }
    private IEnumerator KickInDelay()
    {
        // 少し待つ
        yield return new WaitForSeconds(2f);

        MonoBehaviour last = ball.LastKicker;

        Debug.Log("LastKicker : " + last);

        if (last == null)
            yield break;


        if (last.GetComponent<PlayerController>() != null)
        {
            EnemyAI enemy = FindNearestEnemy();

            if (enemy != null)
            {
                Debug.Log("敵キックイン選手 : " + enemy.name);

                StartCoroutine(SetKickInPlayer(enemy));
            }
        }
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
        NavMeshAgent agent =
            player.GetComponent<NavMeshAgent>();


        if (agent != null)
        {
            // プレイヤー操作中で無効なら有効化
            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            if (!agent.isOnNavMesh)
            {
                agent.Warp(player.transform.position);
            }


            // ボール位置へ移動
            agent.Warp(ball.transform.position);
        }
        else
        {
            player.transform.position =
                ball.transform.position;
        }


        // 少し待つ
        yield return new WaitForSeconds(0.5f);


        // ボール保持
        ball.SetOwner(player);

        GameManager.Instance.EndKickIn();

        Debug.Log(player.name + " が再開");
    }

}