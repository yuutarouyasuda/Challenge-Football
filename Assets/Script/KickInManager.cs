using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class KickInManager : MonoBehaviour
{
    public static KickInManager Instance;

    [SerializeField] private BallController ball;

    private void Awake()
    {
        Instance = this;
    }

    public void StartKickIn()
    {
        if (GameManager.Instance.IskickIn)
            return;

        Debug.Log("キックイン開始");

        GameManager.Instance.StartKickIn();

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        StartCoroutine(KickInDelay());
    }

    private IEnumerator KickInDelay()
    {
        yield return new WaitForSeconds(0.5f);

        MonoBehaviour last = ball.LastKicker;

        if (last == null)
            yield break;

        MonoBehaviour kickInPlayer = null;

        if (last.GetComponent<PlayerController>() != null)
        {
            kickInPlayer = FindNearest<EnemyAI>();
        }
        else if (last.GetComponent<EnemyAI>() != null)
        {
            kickInPlayer = FindNearest<PlayerController>();
        }

        if (kickInPlayer == null)
            yield break;

        Debug.Log("キックイン選手 : " + kickInPlayer.name);

        SetKickInPlayer(kickInPlayer);
    }

    private T FindNearest<T>() where T : MonoBehaviour
    {
        T[] list = FindObjectsByType<T>(FindObjectsSortMode.None);

        T nearest = null;
        float minDistance = Mathf.Infinity;
        foreach (T obj in list)
        {
            // キーパーはキックイン選手にしない
            if (obj.GetComponent<GoalkeeperAI>() != null)
                continue;

            float distance = Vector3.Distance(
                ball.transform.position,
                obj.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = obj;
            }
        }

        return nearest;
    }

    private void SetKickInPlayer(MonoBehaviour player)
    {
        NavMeshAgent agent = player.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            if (!agent.enabled)
                agent.enabled = true;

            if (!agent.isOnNavMesh)
                agent.Warp(player.transform.position);

            agent.Warp(ball.transform.position);
        }
        else
        {
            player.transform.position = ball.transform.position;
        }
        EnemyAI enemy = player.GetComponent<EnemyAI>();
        PlayerController pc = player.GetComponent<PlayerController>();

        ball.SetOwner(player);

        if (enemy != null)
        {
            enemy.StartKickInPlayer();
            enemy.KickInPass();
        }
        else if (pc != null)
        {
            pc.StartKickInPlayer();
        }
        Debug.Log(player.name + " が再開");
    }
}