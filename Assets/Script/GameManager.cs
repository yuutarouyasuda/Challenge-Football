using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using Unity.Multiplayer.PlayMode;
using UnityEngine.AI;
using System.Collections;
public class GameManager : MonoBehaviour
{
    public PlayerController CurrentPlayer {  get; private set; }
    public bool IsKickOff {  get; private set; }
    public bool IsGameStop {  get; private set; }
    public static GameManager Instance;
    [Header("Score")]
    public int homeScore;
    public int awayScore;

    [Header("Match")]
    public float matchTime = 300f;

    [SerializeField] private Transform ballSpawnPoint;

    [SerializeField] private TMP_Text homeScoreText;
    [SerializeField] private TMP_Text awayScoreText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField]private KickOffManager kickOffManager;
    [SerializeField] private float goalRestartDelay = 5f;
    [SerializeField] private float beforeKickOffDelay = 2f;
    private float currentTime;

    private void Awake()
    {
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        currentTime = matchTime;

        UpdateTimerUI();
        UpdateScoreUI();

        kickOffManager.StartKickOff(kickOffManager.GetHomeKickOffPlayer());
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            currentTime = 0;
            UpdateTimerUI();
            EndMatch();
        }
    }
    public void StartKickOffState()
    {
        IsKickOff = true;
    }
    public void EndKickOffState()
    {
        IsKickOff = false;
        Debug.Log("キックオフ終了");
    }
    public void ChangePlayer(PlayerController player)
    {
        if (CurrentPlayer != null)
        {
            CurrentPlayer.IsControlled = false;
            CurrentPlayer.DisableInput();

            NavMeshAgent oldAgent = CurrentPlayer.GetComponent<NavMeshAgent>();
            if (oldAgent != null)
                oldAgent.enabled = true;

            TeammateAI_New oldAI = CurrentPlayer.GetComponent<TeammateAI_New>();
            if (oldAI != null)
                oldAI.enabled = true;

            GoalkeeperAI oldGK = CurrentPlayer.GetComponent<GoalkeeperAI>();

            if (oldGK != null)
                oldGK.enabled = true;
        }

        CurrentPlayer = player;
        CurrentPlayer.IsControlled = true;
        CurrentPlayer.EnableInput();

        NavMeshAgent newAgent = CurrentPlayer.GetComponent<NavMeshAgent>();
        if (newAgent != null)
            newAgent.enabled = false;

        TeammateAI_New newAI = CurrentPlayer.GetComponent<TeammateAI_New>();
        if (newAI != null)
            newAI.enabled = false;

        GoalkeeperAI gk = CurrentPlayer.GetComponent<GoalkeeperAI>();

        if (gk != null)
            gk.enabled = false;
    }
    public void Goal(bool homeGoal, Rigidbody ballRb)
    {
        MonoBehaviour nextKickOffPlayer;


        if (homeGoal)
        {
            awayScore++;
            nextKickOffPlayer = kickOffManager.GetHomeKickOffPlayer();
        }
        else
        {
            homeScore++;
            nextKickOffPlayer = kickOffManager.GetAwayKickOffPlayer();
        }


        UpdateScoreUI();

        IsGameStop = true;

        // 試合停止
        IsKickOff = true;
        Debug.Log("ゴール後停止開始 : " + IsKickOff);


        // すぐ配置
        PlayerController[] players =
            FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController player in players)
        {
            player.ResetPosition();
        }


        EnemyAI[] enemies =
            FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (EnemyAI enemy in enemies)
        {
            enemy.ResetPosition();
        }

        BallController ball =FindAnyObjectByType<BallController>();
        if(ball!=null)
        {
            ball.ResetBall(ballSpawnPoint.position);
        }
        StartCoroutine(RestartAfterGoal(nextKickOffPlayer));
    }
    private IEnumerator RestartAfterGoal(MonoBehaviour nextKickOffPlayer)
    {
        Debug.Log("選手・ボール配置完了");

        // 配置後待機
        yield return new WaitForSeconds(goalRestartDelay);


        // キックオフ準備
        IsGameStop = false;


        // 少し間を作る
        yield return new WaitForSeconds(beforeKickOffDelay);


        kickOffManager.StartKickOff(nextKickOffPlayer);
    }
    /*public void OnBallOwnerChanged(MonoBehaviour owner)
    {
        PlayerController player = owner.GetComponent<PlayerController>();

        if (player != null)
        {
            ChangePlayer(player);
        }
    }*/
    private void UpdateTimerUI()
    {
        int minutes=Mathf.FloorToInt(currentTime/60);
        int seconds=Mathf.FloorToInt(currentTime%60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    private void UpdateScoreUI()
    {
        homeScoreText.text = $"Home : {homeScore}";
        awayScoreText.text = $"Away : {awayScore}";
    }
    private void EndMatch()
    {
        Debug.Log("試合終了");
    }
}
