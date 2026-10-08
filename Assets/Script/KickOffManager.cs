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

    public bool IsKickOff {  get; private set; }

    public void StartKickOff(MonoBehaviour player)
    {
        IsKickOff = true;

        GameManager.Instance.StartKickOffState();

        ball.transform.position = ballPoint.position;
        ball.ClearOwner();

        player.transform.position = kickOffPlayerPoint.position;

        ball.SetOwner(player);

        EnemyAI enemy=player.GetComponent<EnemyAI>();
        if(enemy!=null)
        {
            enemy.SetKickOffPlayer();
        }
        // 自分なら操作
        PlayerController pc = player.GetComponent<PlayerController>();

        if (pc != null)
        {
            GameManager.Instance.ChangePlayer(pc);
        }
        else
        {
            Debug.Log("敵キックオフ");
        }


        Debug.Log(player.name + " がキックオフ");
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