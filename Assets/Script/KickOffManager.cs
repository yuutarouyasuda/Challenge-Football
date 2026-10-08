using UnityEngine;

public class KickOffManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private BallController ball;

    [SerializeField] private MonoBehaviour homeKickOffPlayer;
    [SerializeField] private MonoBehaviour awayKickOffPlayer;

    [Header("Position")]
    [SerializeField] private Transform ballPoint;
    [SerializeField] private Transform kickOffPlayerPoint;
    [Header("KickOff Wait Position")]
    [SerializeField] private Transform homeKickOffWaitPoint;
    [SerializeField] private Transform awayKickOffWaitPoint;
    public bool IsKickOff {  get; private set; }

    public void StartKickOff(MonoBehaviour player)
    {
        IsKickOff = true;

        GameManager.Instance.StartKickOffState();

        ball.transform.position = ballPoint.position;
        ball.ClearOwner();


        // キックオフ選手を中央へ
        player.transform.position = kickOffPlayerPoint.position;


        // プレイヤーがキックオフ
        if (player == homeKickOffPlayer)
        {
            // 相手キックオフ選手を待機位置へ
            awayKickOffPlayer.transform.position = awayKickOffWaitPoint.position;

            PlayerController pc = player.GetComponent<PlayerController>();

            if (pc != null)
            {
                GameManager.Instance.ChangePlayer(pc);
            }
        }
        // 敵がキックオフ
        else if (player == awayKickOffPlayer)
        {
            // プレイヤーキックオフ選手を待機位置へ
            homeKickOffPlayer.transform.position = homeKickOffWaitPoint.position;


            EnemyAI enemy = player.GetComponent<EnemyAI>();

            if (enemy != null)
            {
                enemy.SetKickOffPlayer();
            }
        }


        ball.SetOwner(player);

        Debug.Log(player.name + " がキックオフ");
    }
    private void MoveEnemyKickOffPlayer()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (EnemyAI enemy in enemies)
        {
            // キックオフする敵は除外
            if (enemy.gameObject == null)
                continue;


            // 敵キックオフ担当だけ移動
            enemy.transform.position = awayKickOffWaitPoint.position;
            break;
        }
    }
    private void MovePlayerKickOffPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            player.transform.position = homeKickOffWaitPoint.position;
            break;
        }
    }
    public MonoBehaviour GetHomeKickOffPlayer()
    {
        return homeKickOffPlayer;
    }

    public MonoBehaviour GetAwayKickOffPlayer()
    {
        return awayKickOffPlayer;
    }
    public void EndKickOff()
    {
        IsKickOff = false;

        GameManager.Instance.EndKickOffState();

        Debug.Log("キックオフ終了");
    }
}